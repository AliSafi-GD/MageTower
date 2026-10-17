using UnityEngine;
using UnityEngine.UI;

namespace ArcaneSpire.Combat
{
    /// <summary>Screen-space HUD showing tower HP (uGUI Canvas + legacy Text). Listens to Tower events.</summary>
    public sealed class HudView : MonoBehaviour
    {
        private Text _hpText;
        private Text _waveText;
        private Text _goldText;
        private Tower _tower;
        private WaveModel _waves;
        private GoldWallet _wallet;

        /// <summary>Builds the canvas and subscribes to the tower.</summary>
        public void Initialize(Tower tower, WaveModel waves, GoldWallet wallet, Font font)
        {
            _tower = tower;
            _waves = waves;
            _wallet = wallet;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var textGo = new GameObject("TowerHpText");
            textGo.transform.SetParent(transform, false);
            _hpText = textGo.AddComponent<Text>();
            _hpText.font = font;
            _hpText.fontSize = 48;
            _hpText.fontStyle = FontStyle.Bold;
            _hpText.color = Color.white;
            _hpText.alignment = TextAnchor.UpperLeft;
            _hpText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hpText.verticalOverflow = VerticalWrapMode.Overflow;
            _hpText.raycastTarget = false;
            var rt = _hpText.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(30f, -20f);
            rt.sizeDelta = new Vector2(800f, 80f);

            var waveGo = new GameObject("WaveText");
            waveGo.transform.SetParent(transform, false);
            _waveText = waveGo.AddComponent<Text>();
            _waveText.font = font;
            _waveText.fontSize = 48;
            _waveText.fontStyle = FontStyle.Bold;
            _waveText.color = Color.white;
            _waveText.alignment = TextAnchor.UpperRight;
            _waveText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _waveText.verticalOverflow = VerticalWrapMode.Overflow;
            _waveText.raycastTarget = false;
            // Top-right corner, right-aligned: grows leftward so it cannot clip or collide with the HP text.
            var wrt = _waveText.rectTransform;
            wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(1f, 1f);
            wrt.anchoredPosition = new Vector2(-30f, -20f);
            wrt.sizeDelta = new Vector2(700f, 80f);
            // Gold sits under the HP text (top-left), clear of the wave text (top-right).
            var goldGo = new GameObject("GoldText");
            goldGo.transform.SetParent(transform, false);
            _goldText = goldGo.AddComponent<Text>();
            _goldText.font = font;
            _goldText.fontSize = 48;
            _goldText.fontStyle = FontStyle.Bold;
            _goldText.color = new Color(1f, 0.85f, 0.2f);
            _goldText.alignment = TextAnchor.UpperLeft;
            _goldText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _goldText.verticalOverflow = VerticalWrapMode.Overflow;
            _goldText.raycastTarget = false;
            var grt = _goldText.rectTransform;
            grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0f, 1f);
            grt.anchoredPosition = new Vector2(30f, -85f);
            grt.sizeDelta = new Vector2(800f, 80f);
            wallet.OnGoldChanged += HandleGoldChanged;
            HandleGoldChanged(wallet.Amount);

            waves.OnWaveChanged += HandleWaveChanged;
            HandleWaveChanged(waves.CurrentWave, waves.BestWave);

            tower.OnHpChanged += HandleHpChanged;
            tower.OnDestroyed += HandleDestroyed;
            HandleHpChanged(tower.Hp, tower.MaxHp);
        }

        private void OnDestroy()
        {
            if (_waves != null) _waves.OnWaveChanged -= HandleWaveChanged;
            if (_wallet != null) _wallet.OnGoldChanged -= HandleGoldChanged;
            if (_tower == null) return;
            _tower.OnHpChanged -= HandleHpChanged;
            _tower.OnDestroyed -= HandleDestroyed;
        }

        private void HandleGoldChanged(double gold)
        {
            _goldText.text = $"Gold: {NumberFormat.Short(gold)}";
        }

        private void HandleHpChanged(float hp, float max)
        {
            _hpText.text = $"Tower HP: {NumberFormat.Short(Mathf.CeilToInt(hp))} / {NumberFormat.Short(Mathf.CeilToInt(max))}";
        }

        private void HandleWaveChanged(int wave, int best)
        {
            // Wave 1 starts immediately in EnemySpawner.Initialize; the guard only protects other wiring orders.
            _waveText.text = wave < 1 ? "Wave 1 incoming" : $"Wave: {wave}   Best: {best}";
        }

        private void HandleDestroyed()
        {
            _hpText.text += "  - DESTROYED";
        }
    }
}
