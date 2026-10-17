using System.Collections.Generic;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Spawns the distinct placeholder visual of each spell (pure presentation). Story 004.</summary>
    public sealed class SpellEffects
    {
        private readonly CombatConfig _config;
        private readonly Sprite _square;
        private readonly Sprite _circle;
        private readonly Sprite _ring;

        /// <summary>Creates the effect spawner with its placeholder sprites.</summary>
        public SpellEffects(CombatConfig config, Sprite square, Sprite circle, Sprite ring)
        {
            _config = config;
            _square = square;
            _circle = circle;
            _ring = ring;
        }

        /// <summary>Orange filled circle expanding to the blast radius.</summary>
        public void ShowFireball(Vector2 center, float radius) =>
            SpawnDisc("FireballBlast", _circle, center, radius, _config.FireballColor, 9);

        /// <summary>Light-blue ring pulse expanding to the frost radius.</summary>
        public void ShowFrost(Vector2 center, float radius) =>
            SpawnDisc("FrostPulse", _ring, center, radius, _config.FrostColor, 9);

        /// <summary>Yellow line segments through the points, in order.</summary>
        public void ShowLightning(IReadOnlyList<Vector2> points)
        {
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 from = points[i - 1];
                Vector2 to = points[i];
                Vector2 delta = to - from;
                var go = new GameObject("LightningSegment");
                go.transform.position = (from + to) * 0.5f;
                go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                go.transform.localScale = new Vector3(delta.magnitude, _config.LightningThickness, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _square;
                sr.color = _config.LightningColor;
                sr.sortingOrder = 11;
                go.AddComponent<FadingEffect>().Begin(_config.SpellEffectLifetime, 0f, 1f, 1f);
            }
        }

        private void SpawnDisc(string name, Sprite sprite, Vector2 center, float radius, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            go.transform.localScale = Vector3.one * (radius * 2f); // sprites are 1 unit across
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            go.AddComponent<FadingEffect>().Begin(_config.SpellEffectLifetime, 0f, 0.3f, 1f);
        }
    }
}
