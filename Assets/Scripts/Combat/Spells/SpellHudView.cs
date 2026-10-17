using UnityEngine;
using UnityEngine.UI;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Per-spell cooldown indicator (legacy uGUI Text + Image fill bar) stacked under the gold text, top-left.
    /// Locked spells are greyed ("LOCKED"); unlocked show "READY" or the remaining seconds with a filling bar.
    /// Polls <see cref="SpellBook"/> (no event subscriptions to leak). Must sit under the HUD canvas. Story 004.
    /// </summary>
    public sealed class SpellHudView : MonoBehaviour
    {
        private const float RowWidth = 380f;
        private const float RowHeight = 40f;
        private const float RowGap = 6f;
        private const float TopOffset = 175f; // below HP (y -20) and gold (y -85, 80 high)
        private const float LeftOffset = 30f;
        private const int LockedState = -2;
        private const int ReadyState = -1;

        private static readonly string[] Names = { "Fireball", "Frost", "Lightning" };

        private SpellBook _book;
        private Text[] _texts;
        private RectTransform[] _fills;
        private int[] _lastState;

        /// <summary>Builds the rows under this object's canvas.</summary>
        public void Initialize(SpellBook book, CombatConfig config, Font font)
        {
            _book = book;
            int n = SpellBook.SpellCount;
            _texts = new Text[n];
            _fills = new RectTransform[n];
            _lastState = new int[n];
            for (int i = 0; i < n; i++)
            {
                _lastState[i] = int.MinValue;
                var rowGo = new GameObject("SpellRow_" + Names[i], typeof(RectTransform));
                rowGo.transform.SetParent(transform, false);
                var bg = rowGo.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.55f);
                bg.raycastTarget = false;
                var rrt = bg.rectTransform;
                rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0f, 1f);
                rrt.anchoredPosition = new Vector2(LeftOffset, -(TopOffset + i * (RowHeight + RowGap)));
                rrt.sizeDelta = new Vector2(RowWidth, RowHeight);

                var fillGo = new GameObject("Fill", typeof(RectTransform));
                fillGo.transform.SetParent(rowGo.transform, false);
                var fill = fillGo.AddComponent<Image>();
                Color c = config.SpellColor((SpellType)i);
                fill.color = new Color(c.r, c.g, c.b, 0.85f);
                fill.raycastTarget = false;
                var frt = fill.rectTransform;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = new Vector2(0f, 1f);
                frt.offsetMin = frt.offsetMax = Vector2.zero;
                _fills[i] = frt;

                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(rowGo.transform, false);
                var text = textGo.AddComponent<Text>();
                text.font = font;
                text.fontSize = 26;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleLeft;
                text.color = Color.white;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false;
                var trt = text.rectTransform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(10f, 0f);
                trt.offsetMax = new Vector2(-10f, 0f);
                _texts[i] = text;
            }
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (_book == null) return;
            for (int i = 0; i < _texts.Length; i++)
            {
                var type = (SpellType)i;
                int state;
                float fraction;
                if (!_book.IsUnlocked(type))
                {
                    state = LockedState;
                    fraction = 0f;
                }
                else
                {
                    float remaining = _book.Remaining(type);
                    float cd = _book.Cooldown(type);
                    fraction = cd > 0f ? Mathf.Clamp01(1f - remaining / cd) : 1f;
                    state = remaining <= 0f ? ReadyState : Mathf.CeilToInt(remaining * 10f); // tenths of a second
                }
                _fills[i].anchorMax = new Vector2(fraction, 1f);
                if (state == _lastState[i]) continue; // rebuild the string only when the shown value changes
                _lastState[i] = state;
                _texts[i].color = state == LockedState ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
                _texts[i].text = state == LockedState ? Names[i] + "  LOCKED"
                    : state == ReadyState ? Names[i] + "  READY"
                    : Names[i] + "  " + (state / 10) + "." + (state % 10) + "s";
            }
        }
    }
}
