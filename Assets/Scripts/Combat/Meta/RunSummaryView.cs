using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Full-screen run-summary panel (uGUI + legacy Text). <see cref="RunLifecycle"/> shows it automatically when
    /// the tower is destroyed (Story 006, design/game-brief.md MVP feature 6): wave reached, the best-wave record
    /// (highlighted gold + bold if this run set a new one) and the run currency earned this run / in total.
    /// From here: [T] opens the skill tree, [N] starts a new run directly, skipping it (number keys 1-9,0 are
    /// already the shop/spell/rune hotkeys, so this screen uses letters). Keys only act while this GameObject is
    /// active, which <see cref="RunLifecycle"/> already controls, so no separate lock flag is needed. Reads nothing
    /// but the values passed to <see cref="Show"/>; contains no game rules.
    /// </summary>
    public sealed class RunSummaryView : MonoBehaviour
    {
        /// <summary>Raised when the player chooses to open the skill tree screen.</summary>
        public event System.Action OnOpenSkillTree;
        /// <summary>Raised when the player chooses to start a new run directly.</summary>
        public event System.Action OnStartNewRun;

        private static readonly Color NormalColor = Color.white;
        private static readonly Color HighlightColor = new Color(1f, 0.85f, 0.2f);

        private Text _waveText;
        private Text _bestText;
        private Text _earnedText;
        private Text _totalText;

        /// <summary>Builds the panel. Call once; toggle visibility with <see cref="GameObject.SetActive"/>.</summary>
        public void Initialize(Font font)
        {
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
            var brt = bg.rectTransform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            Text title = CreateLabel("Title", font, 56, FontStyle.Bold, Color.white, new Vector2(0.5f, 0.76f));
            title.text = "RUN OVER";
            _waveText = CreateLabel("WaveText", font, 38, FontStyle.Normal, Color.white, new Vector2(0.5f, 0.60f));
            _bestText = CreateLabel("BestText", font, 38, FontStyle.Normal, Color.white, new Vector2(0.5f, 0.52f));
            _earnedText = CreateLabel("EarnedText", font, 34, FontStyle.Normal, HighlightColor, new Vector2(0.5f, 0.42f));
            _totalText = CreateLabel("TotalText", font, 28, FontStyle.Normal, HighlightColor, new Vector2(0.5f, 0.35f));

            CreateActionButton("SkillTreeButton", font, new Vector2(-190f, 60f), "[T] Skill Tree", () => OnOpenSkillTree?.Invoke());
            CreateActionButton("NewRunButton", font, new Vector2(190f, 60f), "[N] Start New Run", () => OnStartNewRun?.Invoke());
        }

        private Text CreateLabel(string name, Font font, int fontSize, FontStyle style, Color color, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1100f, 60f);
            return text;
        }

        private void CreateActionButton(string name, Font font, Vector2 position, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(340f, 80f);
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
        }

        /// <summary>Fills in this run's results and highlights a new best wave.</summary>
        public void Show(int waveReached, int bestWave, bool isNewBest, double currencyEarnedThisRun, double totalCurrency)
        {
            _waveText.text = $"Wave reached: {waveReached}";
            _bestText.text = isNewBest ? $"NEW BEST WAVE: {bestWave}!" : $"Best wave: {bestWave}";
            _bestText.color = isNewBest ? HighlightColor : NormalColor;
            _bestText.fontStyle = isNewBest ? FontStyle.Bold : FontStyle.Normal;
            _earnedText.text = $"Run currency earned: {NumberFormat.Short(currencyEarnedThisRun)}";
            SetTotalCurrency(totalCurrency);
        }

        /// <summary>Updates only the total-currency line (e.g. after skill-tree purchases made from this summary).</summary>
        public void SetTotalCurrency(double totalCurrency) =>
            _totalText.text = $"Total currency: {NumberFormat.Short(totalCurrency)}";

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb[Key.T].wasPressedThisFrame) OnOpenSkillTree?.Invoke();
            if (kb[Key.N].wasPressedThisFrame) OnStartNewRun?.Invoke();
        }
    }
}
