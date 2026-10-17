using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Bottom-of-screen shop panel (uGUI + legacy Text), two rows. Upper row: one button per <see cref="UpgradeType"/>
    /// showing name, level, effect and next cost (or MAX); keys 1-4 buy. Lower row (Story 004): one unlock button per
    /// <see cref="SpellType"/> showing cost or UNLOCKED; keys 5-7 buy. Buttons are disabled when gold is insufficient,
    /// maxed or owned. Reads <see cref="ShopModel"/> only; contains no game rules.
    /// Must be added to the same GameObject as the HUD canvas (or any object under a Canvas).
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        private static readonly Key[] Hotkeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        private static readonly Key[] SpellHotkeys = { Key.Digit5, Key.Digit6, Key.Digit7 };
        private static readonly string[] Names = { "Damage", "Attack Speed", "Tower HP", "Crit Chance" };
        private static readonly string[] SpellNames = { "Fireball", "Frost", "Lightning" };
        private static readonly string[] SpellBlurbs = { "area damage", "slows enemies", "chain damage" };

        private const float UpgradeRowHeight = 100f;
        private const float SpellRowHeight = 70f;
        private const float RowGap = 8f;
        private const float PanelBottom = 16f;
        private const float PanelSideMargin = 20f;
        private const float ButtonGap = 20f;

        private ShopModel _shop;
        private CombatConfig _config;
        private Button[] _buttons;
        private Text[] _labels;
        private Button[] _spellButtons;
        private Text[] _spellLabels;
        private bool _locked;

        /// <summary>Builds the panel under this object's canvas and ensures an EventSystem exists.</summary>
        public void Initialize(ShopModel shop, CombatConfig config, Font font)
        {
            _shop = shop;
            _config = config;
            EnsureEventSystem();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // Each row is stretched across the canvas width; its layout group splits it evenly between the buttons,
            // so the rows always fit the screen (16:9, 4:3, narrow windows).
            int n = ShopModel.UpgradeCount;
            _buttons = new Button[n];
            _labels = new Text[n];
            Transform upgradeRow = CreateRow("ShopPanel", PanelBottom + SpellRowHeight + RowGap, UpgradeRowHeight);
            for (int i = 0; i < n; i++)
            {
                UpgradeType type = (UpgradeType)i;
                CreateButton(upgradeRow, "ShopButton_" + type, font, () => Buy(type), out _buttons[i], out _labels[i]);
            }

            int sn = SpellBook.SpellCount;
            _spellButtons = new Button[sn];
            _spellLabels = new Text[sn];
            Transform spellRow = CreateRow("SpellShopPanel", PanelBottom, SpellRowHeight);
            for (int i = 0; i < sn; i++)
            {
                SpellType type = (SpellType)i;
                CreateButton(spellRow, "SpellButton_" + type, font, () => BuySpell(type), out _spellButtons[i], out _spellLabels[i]);
            }

            _shop.OnChanged += Refresh;
            _shop.Wallet.OnGoldChanged += HandleGold;
            Refresh();
        }

        private Transform CreateRow(string name, float bottom, float height)
        {
            var panelGo = new GameObject(name, typeof(RectTransform));
            panelGo.transform.SetParent(transform, false);
            var prt = (RectTransform)panelGo.transform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.offsetMin = new Vector2(PanelSideMargin, bottom);
            prt.offsetMax = new Vector2(-PanelSideMargin, bottom + height);
            var layout = panelGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = ButtonGap;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return panelGo.transform;
        }

        internal static void CreateButton(Transform row, string name, Font font, UnityEngine.Events.UnityAction onClick,
            out Button button, out Text text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(row, false);
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var element = go.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.minWidth = 0f;
            button = go.AddComponent<Button>();
            button.targetGraphic = image;
            // No keyboard/gamepad navigation: Space/Enter/WASD must never trigger purchases (keys 1-7 do).
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.25f, 0.45f, 0.92f);
            colors.highlightedColor = new Color(0.3f, 0.38f, 0.65f, 0.95f);
            colors.pressedColor = new Color(0.15f, 0.2f, 0.35f, 1f);
            colors.selectedColor = colors.normalColor; // clicked buttons must not stay highlighted/white
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 28;
            text.resizeTextForBestFit = true; // shrink rather than clip in narrow windows
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var trt = text.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8f, 4f);
            trt.offsetMax = new Vector2(-8f, -4f);
        }

        /// <summary>Disables all purchasing (e.g. when the tower has fallen).</summary>
        public void SetLocked(bool locked)
        {
            _locked = locked;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_shop == null) return;
            _shop.OnChanged -= Refresh;
            _shop.Wallet.OnGoldChanged -= HandleGold;
        }

        private void Update()
        {
            if (_locked) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            for (int i = 0; i < Hotkeys.Length && i < ShopModel.UpgradeCount; i++)
            {
                if (kb[Hotkeys[i]].wasPressedThisFrame) Buy((UpgradeType)i);
            }
            for (int i = 0; i < SpellHotkeys.Length && i < SpellBook.SpellCount; i++)
            {
                if (kb[SpellHotkeys[i]].wasPressedThisFrame) BuySpell((SpellType)i);
            }
        }

        private void Buy(UpgradeType type)
        {
            if (_locked) return;
            _shop.TryBuy(type);
        }

        private void BuySpell(SpellType type)
        {
            if (_locked) return;
            _shop.TryBuySpell(type);
        }

        private void HandleGold(double gold) => Refresh();

        private void Refresh()
        {
            if (_shop == null) return;
            for (int i = 0; i < _buttons.Length; i++)
            {
                var type = (UpgradeType)i;
                bool maxed = _shop.IsMaxed(type);
                _buttons[i].interactable = !_locked && !maxed && _shop.CanBuy(type);
                string cost = maxed ? "MAX" : "Cost: " + NumberFormat.Short(_shop.GetCost(type));
                _labels[i].text = $"[{i + 1}] {At(Names, i, type.ToString())}  Lv {_shop.GetLevel(type)}\n{EffectSummary(type, maxed)}\n{cost}";
            }
            for (int i = 0; i < _spellButtons.Length; i++)
            {
                var type = (SpellType)i;
                bool owned = _shop.IsSpellOwned(type);
                _spellButtons[i].interactable = !_locked && !owned && _shop.CanBuySpell(type);
                string second = owned ? "UNLOCKED" : "Unlock: " + NumberFormat.Short(_shop.GetSpellCost(type));
                _spellLabels[i].text = $"[{i + 5}] {At(SpellNames, i, type.ToString())} ({At(SpellBlurbs, i, "")})\n{second}";
            }
        }

        // Guarded table lookup: an enum entry added without a label falls back instead of throwing.
        private static string At(string[] table, int i, string fallback) => i < table.Length ? table[i] : fallback;

        private string EffectSummary(UpgradeType type, bool maxed)
        {
            int level = _shop.GetLevel(type);
            switch (type)
            {
                case UpgradeType.Damage:
                    return $"+{NumberFormat.Short(_config.DamageBonusPerLevel * 100f)}% damage";
                case UpgradeType.AttackSpeed:
                    return $"+{NumberFormat.Short(_config.AttackSpeedBonusPerLevel * 100f)}% attack speed";
                case UpgradeType.TowerHp:
                    return $"+{NumberFormat.Short(_config.TowerHpPerLevel)} max HP";
                default:
                    float now = UpgradeEffects.CritChance(_config.BaseCritChance, _config.CritChancePerLevel, level, _config.MaxCritChance);
                    string mult = _config.CritDamageMultiplier.ToString("0.#", CultureInfo.InvariantCulture);
                    string nowText = NumberFormat.Short(now * 100.0);
                    if (maxed) return $"Crit {nowText}% (x{mult})";
                    float after = UpgradeEffects.CritChance(_config.BaseCritChance, _config.CritChancePerLevel, level + 1, _config.MaxCritChance);
                    return $"Crit {nowText}% > {NumberFormat.Short(after * 100.0)}% (x{mult})";
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
