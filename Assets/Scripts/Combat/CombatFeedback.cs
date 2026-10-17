using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Spawns floating damage numbers, hit bursts and attack bolts. Pure presentation.</summary>
    public sealed class CombatFeedback
    {
        private readonly CombatConfig _config;
        private readonly Sprite _square;
        private readonly Sprite _circle;
        private readonly Font _font;

        /// <summary>Creates the feedback spawner with its placeholder art and font.</summary>
        public CombatFeedback(CombatConfig config, Sprite square, Sprite circle, Font font)
        {
            _config = config;
            _square = square;
            _circle = circle;
            _font = font;
        }

        /// <summary>Shows a rising damage number and a hit burst at the enemy.</summary>
        public void ShowHit(Enemy enemy, float damage, bool isCrit = false)
        {
            Vector3 pos = enemy.transform.position;

            var numberGo = new GameObject("DamageNumber");
            numberGo.transform.position = pos + Vector3.up * (_config.EnemySize * 0.6f);
            var text = numberGo.AddComponent<TextMesh>();
            text.font = _font;
            text.text = NumberFormat.Short(damage);
            text.fontSize = 64;
            text.characterSize = _config.DamageNumberCharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = isCrit ? _config.CritNumberColor : _config.DamageNumberColor;
            var mr = numberGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _font.material;
            mr.sortingOrder = 20;
            numberGo.AddComponent<FadingEffect>().Begin(_config.DamageNumberLifetime, _config.DamageNumberRiseSpeed, 1.4f * (isCrit ? _config.CritNumberScale : 1f), isCrit ? _config.CritNumberScale : 1f);

            var burstGo = new GameObject("HitBurst");
            burstGo.transform.position = pos;
            burstGo.transform.localScale = Vector3.one * _config.HitBurstSize;
            var sr = burstGo.AddComponent<SpriteRenderer>();
            sr.sprite = _circle;
            sr.color = _config.HitFlashColor;
            sr.sortingOrder = 10;
            burstGo.AddComponent<FadingEffect>().Begin(_config.HitBurstLifetime, 0f, 0.4f, 1.2f);
        }

        /// <summary>Shows a brief bolt line between two points.</summary>
        public void ShowBolt(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            var go = new GameObject("Bolt");
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(delta.magnitude, _config.BoltThickness, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = _config.BoltColor;
            sr.sortingOrder = 8;
            go.AddComponent<FadingEffect>().Begin(_config.BoltLifetime, 0f, 1f, 1f);
        }
    }
}
