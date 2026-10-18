using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Rune panel (uGUI + legacy Text): a narrow column on the right edge, below the wave text and above the
    /// playfield (enemies walk along the ground at the bottom). Header shows "Runes  Slots n/m"; one button per
    /// <see cref="RuneType"/> shows name, effect and state (Locked: cost / Unlocked: Equip / Equipped: Unequip).
    /// Click or keys 8, 9, 0 call <see cref="RuneBook.Activate"/> (unlock then equip, or toggle equip). With all slots
    /// full a locked rune is unlock-only and says "(no free slot)"; an unlocked one says "no free slot" and is disabled.
    /// Buttons are also disabled when locked and unaffordable. Equipped buttons use their own bright-green colour block
    /// (not a tint over the dark normal colour) plus bright bold "EQUIPPED" text. Reads <see cref="RuneBook"/> only.
    /// Strings are built once; labels are rewritten only when a rune's state changes. Must sit under the HUD canvas.
    /// </summary>
    public sealed class RuneView : MonoBehaviour
    {
        private static readonly Key[] Hotkeys = { Key.Digit8, Key.Digit9, Key.Digit0 };
        private static readonly string[] HotkeyLabels = { "8", "9", "0" };
        private static readonly string[] Names = { "Might", "Greed", "Haste" };

        private const float PanelWidth = 330f;
        private const float RightMargin = 30f;
        private const float TopOffset = 120f; // below the wave text (y -20, 80 high)
        private const float HeaderHeight = 44f;
        private const float ButtonHeight = 92f;
        private const float Gap = 8f;

        private const int StateLocked = 0;
        private const int StateLockedNoSlot = 1;
        private const int StateUnlocked = 2;
        private const int StateUnlockedNoSlot = 3;
        private const int StateEquipped = 4;
        private const int StateCount = 5;

        private static readonly Color EquippedText = new Color(1f, 1f, 0.55f, 1f);

        private RuneBook _book;
        private Text _header;
        private Button[] _buttons;
        private ColorBlock _normalColors;
        private ColorBlock _equippedColors;
        private Text[] _labels;
        private string[][] _texts; // [rune][state]
        private string[] _slotTexts; // [equipped count]
        private int[] _lastKey;
        private int _lastSlots = -1;
        private bool _locked;

        /// <summary>Builds the panel under this object's canvas.</summary>
        public void Initialize(RuneBook book, Font font)
        {
            _book = book;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            int n = RuneBook.RuneCount;
            _buttons = new Button[n];
            _labels = new Text[n];
            _texts = new string[n][];
            _lastKey = new int[n];
            _slotTexts = new string[book.SlotCount + 1];
            for (int i = 0; i < _slotTexts.Length; i++) _slotTexts[i] = "Runes   Slots " + i + "/" + book.SlotCount;

            var panelGo = new GameObject("RunePanel", typeof(RectTransform));
            panelGo.transform.SetParent(transform, false);
            var prt = (RectTransform)panelGo.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f);
            prt.anchoredPosition = new Vector2(-RightMargin, -TopOffset);
            prt.sizeDelta = new Vector2(PanelWidth, HeaderHeight + n * (ButtonHeight + Gap));
            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = Gap;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(panelGo.transform, false);
            _header = headerGo.AddComponent<Text>();
            _header.font = font;
            _header.fontSize = 32;
            _header.fontStyle = FontStyle.Bold;
            _header.alignment = TextAnchor.MiddleRight;
            _header.color = Color.white;
            _header.horizontalOverflow = HorizontalWrapMode.Overflow;
            _header.verticalOverflow = VerticalWrapMode.Overflow;
            _header.raycastTarget = false;
            headerGo.AddComponent<LayoutElement>().preferredHeight = HeaderHeight;

            for (int i = 0; i < n; i++)
            {
                RuneType type = (RuneType)i;
                ShopView.CreateButton(panelGo.transform, "RuneButton_" + type, font, () => Activate(type),
                    out _buttons[i], out _labels[i]);
                _buttons[i].GetComponent<LayoutElement>().preferredHeight = ButtonHeight;
                if (i == 0) BuildColorBlocks(_buttons[0].colors);
                _labels[i].fontSize = 24;
                _labels[i].resizeTextMaxSize = 24;

                // Guarded lookups: a rune added to the enum without a table entry falls back instead of throwing.
                string hotkey = i < HotkeyLabels.Length ? "[" + HotkeyLabels[i] + "] " : "";
                string name = i < Names.Length ? Names[i] : type.ToString();
                string head = hotkey + name + "  " + Describe(book.ModifierType(type), book.Value(type));
                string cost = NumberFormat.Short(book.UnlockCost(type));
                _texts[i] = new string[StateCount];
                _texts[i][StateLocked] = head + "\nLocked: " + cost + " gold";
                _texts[i][StateLockedNoSlot] = head + "\nUnlock: " + cost + " (no free slot)";
                _texts[i][StateUnlocked] = head + "\nUnlocked - Equip";
                _texts[i][StateUnlockedNoSlot] = head + "\nUnlocked - no free slot";
                _texts[i][StateEquipped] = head + "\nEQUIPPED - Unequip";
                _lastKey[i] = -1;
            }

            _book.OnChanged += Refresh;
            _book.Wallet.OnGoldChanged += HandleGold;
            Refresh();
        }

        /// <summary>Disables all rune actions (e.g. when the tower has fallen).</summary>
        public void SetLocked(bool locked)
        {
            _locked = locked;
            Refresh();
        }

        private void BuildColorBlocks(ColorBlock normal)
        {
            _normalColors = normal;
            _equippedColors = normal;
            _equippedColors.normalColor = new Color(0.1f, 0.62f, 0.22f, 1f);
            _equippedColors.highlightedColor = new Color(0.2f, 0.78f, 0.32f, 1f);
            _equippedColors.pressedColor = new Color(0.06f, 0.45f, 0.15f, 1f);
            _equippedColors.selectedColor = _equippedColors.normalColor;
            _equippedColors.disabledColor = new Color(0.1f, 0.35f, 0.15f, 0.85f);
        }

        private static string Describe(RuneModifierType type, float value)
        {
            string pct = NumberFormat.Short(value * 100f) + "%";
            switch (type)
            {
                case RuneModifierType.Damage: return "+" + pct + " dmg";
                case RuneModifierType.Gold: return "+" + pct + " gold";
                default: return "-" + pct + " cooldown";
            }
        }

        private void OnDestroy()
        {
            if (_book == null) return;
            _book.OnChanged -= Refresh;
            _book.Wallet.OnGoldChanged -= HandleGold;
        }

        private void Update()
        {
            if (_locked || ModalGate.IsOpen) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            for (int i = 0; i < Hotkeys.Length && i < RuneBook.RuneCount; i++)
                if (kb[Hotkeys[i]].wasPressedThisFrame) Activate((RuneType)i);
        }

        private void Activate(RuneType type)
        {
            if (_locked) return;
            _book.Activate(type);
        }

        private void HandleGold(double gold) => Refresh();

        private void Refresh()
        {
            if (_book == null) return;
            if (_lastSlots != _book.EquippedCount)
            {
                _lastSlots = _book.EquippedCount;
                _header.text = _slotTexts[Mathf.Clamp(_lastSlots, 0, _slotTexts.Length - 1)];
            }
            for (int i = 0; i < _buttons.Length; i++)
            {
                var type = (RuneType)i;
                bool slotFree = _book.EquippedCount < _book.SlotCount;
                int state = _book.IsEquipped(type) ? StateEquipped
                    : _book.IsUnlocked(type) ? (slotFree ? StateUnlocked : StateUnlockedNoSlot)
                    : (slotFree ? StateLocked : StateLockedNoSlot);
                bool usable = state == StateEquipped
                    || state == StateUnlocked
                    || ((state == StateLocked || state == StateLockedNoSlot) && _book.CanUnlock(type));
                bool interactable = !_locked && usable;
                int key = state * 2 + (interactable ? 1 : 0);
                if (key == _lastKey[i]) continue; // rewrite only on change
                _lastKey[i] = key;
                _buttons[i].interactable = interactable;
                _buttons[i].colors = state == StateEquipped ? _equippedColors : _normalColors;
                _labels[i].color = state == StateEquipped ? EquippedText : Color.white;
                _labels[i].fontStyle = state == StateEquipped ? FontStyle.Bold : FontStyle.Normal;
                _labels[i].text = _texts[i][state];
            }
        }
    }
}
