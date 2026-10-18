using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Full-screen permanent skill-tree panel (uGUI + legacy Text) (Story 006, design/game-brief.md MVP feature 6).
    /// Lists every <see cref="SkillNodeType"/> node with its name, per-level effect, current/max level and next-level
    /// cost (or MAX), plus the current run-currency balance. Between runs, keys Q/W/E/R/F buy a level of nodes 1-5
    /// (number keys 1-9,0 are already the shop/spell/rune hotkeys) and N starts a new run. Opened mid-run (K) it is
    /// view-only: no purchases, so every permanent upgrade applies from the start of the next run. K (Back) is
    /// handled by RunLifecycle.
    /// Keys only act while this GameObject is active, which <see cref="RunLifecycle"/> already controls. Reads
    /// <see cref="SkillTree"/> and <see cref="MetaWallet"/> only; contains no game rules.
    /// </summary>
    public sealed class SkillTreeView : MonoBehaviour
    {
        private static readonly Key[] Hotkeys = { Key.Q, Key.W, Key.E, Key.R, Key.F };
        private static readonly string[] HotkeyLabels = { "Q", "W", "E", "R", "F" };
        private static readonly string[] Names = { "Arcane Power", "Swift Hands", "Quick Recovery", "Golden Touch", "Tower Bastion" };

        /// <summary>Raised when the player chooses to start a new run from this screen (only offered once a run is over).</summary>
        public event System.Action OnStartNewRun;
        /// <summary>Raised when the player closes the screen with the Back button.</summary>
        public event System.Action OnClose;

        private SkillTree _tree;
        private MetaWallet _wallet;
        private Text _header;
        private Button[] _buttons;
        private Text[] _labels;
        private Image _backdrop;
        private GameObject _newRunButton;
        private bool _runOver = true;

        /// <summary>Builds the panel. Call once; toggle visibility with <see cref="GameObject.SetActive"/>.</summary>
        public void Initialize(SkillTree tree, MetaWallet wallet, Font font)
        {
            _tree = tree;
            _wallet = wallet;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the in-run HUD
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var bgGo = new GameObject("Backdrop", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);
            _backdrop = bg;
            var brt = bg.rectTransform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(transform, false);
            _header = headerGo.AddComponent<Text>();
            _header.font = font;
            _header.fontSize = 44;
            _header.fontStyle = FontStyle.Bold;
            _header.alignment = TextAnchor.MiddleCenter;
            _header.color = Color.white;
            _header.horizontalOverflow = HorizontalWrapMode.Overflow;
            _header.verticalOverflow = VerticalWrapMode.Overflow;
            _header.raycastTarget = false;
            var hrt = _header.rectTransform;
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -40f);
            hrt.sizeDelta = new Vector2(0f, 70f);

            int n = SkillTree.NodeCount;
            _buttons = new Button[n];
            _labels = new Text[n];

            var listGo = new GameObject("NodeList", typeof(RectTransform));
            listGo.transform.SetParent(transform, false);
            var lrt = (RectTransform)listGo.transform;
            lrt.anchorMin = new Vector2(0.22f, 0.16f);
            lrt.anchorMax = new Vector2(0.78f, 0.82f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var layout = listGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < n; i++)
            {
                var type = (SkillNodeType)i;
                ShopView.CreateButton(listGo.transform, "SkillNode_" + type, font, () => Buy(type), out _buttons[i], out _labels[i]);
                _buttons[i].GetComponent<LayoutElement>().preferredHeight = 96f;
                _labels[i].fontSize = 22;
                _labels[i].resizeTextMaxSize = 22;
            }

            CreateBottomButton("BackButton", font, new Vector2(-200f, 40f), "[K] Back", () => OnClose?.Invoke());
            _newRunButton = CreateBottomButton("NewRunButton", font, new Vector2(200f, 40f), "[N] Start New Run",
                () => OnStartNewRun?.Invoke());

            _tree.OnChanged += Refresh;
            _wallet.OnCurrencyChanged += HandleCurrencyChanged;
            Refresh();
        }

        private GameObject CreateBottomButton(string name, Font font, Vector2 position, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(360f, 80f);
            ShopView.CreateButton(go.transform, name + "Inner", font, onClick, out Button button, out Text text);
            // CreateButton leaves its RectTransform at the default 100x100; stretch it over the sized parent.
            var brt = (RectTransform)button.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            text.text = label;
            text.fontSize = 26;
            text.resizeTextMaxSize = 26;
            return go;
        }

        /// <summary>
        /// Sets how the screen was reached. Between runs (<paramref name="runOver"/> true) it is a full backdrop with a
        /// Start New Run button and purchases enabled; opened mid-run it is a lighter backdrop (playfield stays visible,
        /// combat keeps running) and view-only: no purchases and no new run, so permanent upgrades only change between
        /// runs and a run can never be abandoned by accident.
        /// </summary>
        public void SetContext(bool runOver)
        {
            _runOver = runOver;
            _backdrop.color = new Color(0f, 0f, 0f, runOver ? 0.85f : 0.55f);
            _newRunButton.SetActive(runOver);
            Refresh();
        }

        private void OnEnable() => Refresh();

        private void OnDestroy()
        {
            if (_tree != null) _tree.OnChanged -= Refresh;
            if (_wallet != null) _wallet.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (_runOver)
                for (int i = 0; i < Hotkeys.Length && i < SkillTree.NodeCount; i++)
                    if (kb[Hotkeys[i]].wasPressedThisFrame) Buy((SkillNodeType)i);
            if (_runOver && kb[Key.N].wasPressedThisFrame) OnStartNewRun?.Invoke();
        }

        private void Buy(SkillNodeType type)
        {
            if (_runOver) _tree.TryBuy(type, _wallet); // view-only mid-run
        }

        private void HandleCurrencyChanged(double amount) => Refresh();

        private void Refresh()
        {
            // Skipped while hidden; OnEnable refreshes when the screen is shown again.
            if (_tree == null || !isActiveAndEnabled) return;
            _header.text = _runOver
                ? $"Skill Tree    Currency: {NumberFormat.Short(_wallet.Amount)}"
                : $"Skill Tree (view only - buy after the run)    Currency: {NumberFormat.Short(_wallet.Amount)}";
            for (int i = 0; i < _buttons.Length; i++)
            {
                var type = (SkillNodeType)i;
                _buttons[i].interactable = _runOver && _tree.CanBuy(type, _wallet);
                string hotkey = i < HotkeyLabels.Length ? "[" + HotkeyLabels[i] + "] " : "";
                string name = i < Names.Length ? Names[i] : type.ToString();
                string cost = _tree.IsMaxed(type) ? "MAX" : "Next level: " + NumberFormat.Short(_tree.Cost(type));
                if (!_runOver && !_tree.IsMaxed(type)) cost += "  (Buy after the run)";
                _labels[i].text = $"{hotkey}{name}   Lv {_tree.Level(type)}/{_tree.MaxLevel}\n{EffectSummary(type)}\n{cost}";
            }
        }

        private string EffectSummary(SkillNodeType type)
        {
            float v = _tree.Value(type);
            string per = " per level";
            switch (type)
            {
                case SkillNodeType.ArcanePower: return $"+{NumberFormat.Short(v * 100f)}% damage" + per;
                case SkillNodeType.SwiftHands: return $"+{NumberFormat.Short(v * 100f)}% attack speed" + per;
                case SkillNodeType.QuickRecovery: return $"-{NumberFormat.Short(v * 100f)}% cooldown" + per;
                case SkillNodeType.GoldenTouch: return $"+{NumberFormat.Short(v * 100f)}% gold" + per;
                default: return $"+{NumberFormat.Short(v)} max HP" + per;
            }
        }
    }
}
