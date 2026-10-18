using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Allocation-free "a modal popup is open" check (Story 007). Gameplay hotkey handlers (shop, spells, runes,
    /// skill tree, run summary, [K]) return early while <see cref="IsOpen"/> is true.
    /// </summary>
    public static class ModalGate
    {
        /// <summary>True while a modal popup (the welcome-back popup) is shown.</summary>
        public static bool IsOpen { get; internal set; }

        // Statics survive Play Mode entry when domain reload is disabled; start every session closed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => IsOpen = false;
    }

    /// <summary>
    /// Welcome-back popup (uGUI + legacy Text, same style as <see cref="RunSummaryView"/>): time away, a cap note if the
    /// absence exceeded the offline maximum, and the run currency earned while away. Story 007
    /// (design/game-brief.md MVP feature 7). Dismissed with the [C] Continue button or the C / Escape key (Enter and
    /// Space are avoided because buttons use Navigation.Mode.None). Never auto-dismisses. While shown the game is paused
    /// (<see cref="Time.timeScale"/> = 0, previous value restored on dismiss/disable/destroy) and <see cref="ModalGate"/>
    /// is open so gameplay hotkeys are ignored; the full-screen backdrop also blocks clicks on the UI beneath. Contains
    /// no game rules.
    /// </summary>
    public sealed class OfflineProgressView : MonoBehaviour
    {
        /// <summary>Raised when the player dismisses the popup.</summary>
        public event System.Action OnDismissed;

        private static readonly Color HighlightColor = new Color(1f, 0.85f, 0.2f);

        private Text _awayText;
        private Text _capText;
        private Text _earnedText;
        private bool _pausing;
        private float _previousTimeScale = 1f;

        /// <summary>Builds the panel. Call once; toggle visibility with <see cref="GameObject.SetActive"/>.</summary>
        public void Initialize(Font font)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // above the run summary (100) and skill tree
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
            title.text = "WELCOME BACK";
            _awayText = CreateLabel("AwayText", font, 38, FontStyle.Normal, Color.white, new Vector2(0.5f, 0.60f));
            _capText = CreateLabel("CapText", font, 28, FontStyle.Italic, new Color(1f, 0.6f, 0.4f), new Vector2(0.5f, 0.52f));
            _earnedText = CreateLabel("EarnedText", font, 40, FontStyle.Bold, HighlightColor, new Vector2(0.5f, 0.42f));

            var go = new GameObject("ContinueButton", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 60f);
            rt.sizeDelta = new Vector2(340f, 80f);
            ShopView.CreateButton(go.transform, "ContinueButtonInner", font, Dismiss, out Button button, out Text text);
            var inner = (RectTransform)button.transform;
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = Vector2.zero;
            inner.offsetMax = Vector2.zero;
            text.text = "[C] Continue";
            text.fontSize = 26;
            text.resizeTextMaxSize = 26;
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

        /// <summary>Fills in the result and its cap (hours) and makes the popup visible.</summary>
        public void Show(OfflineResult result, float maxHours)
        {
            _awayText.text = $"You were away for {OfflineProgress.FormatDuration(result.ElapsedSeconds)}";
            _capText.text = result.Capped ? $"Offline gains capped at {maxHours.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}h" : string.Empty;
            _earnedText.text = $"Run currency earned: +{NumberFormat.Short(result.Currency)}";
            gameObject.SetActive(true);
            if (!_pausing)
            {
                _pausing = true;
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            ModalGate.IsOpen = true;
        }

        private void Dismiss()
        {
            gameObject.SetActive(false); // OnDisable restores the time scale and closes the gate
            OnDismissed?.Invoke();
        }

        private void ReleasePause()
        {
            if (_pausing)
            {
                _pausing = false;
                Time.timeScale = _previousTimeScale;
            }
            ModalGate.IsOpen = false;
        }

        private void OnDisable() => ReleasePause();

        private void OnDestroy() => ReleasePause();

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb[Key.C].wasPressedThisFrame || kb[Key.Escape].wasPressedThisFrame) Dismiss();
        }
    }
}
