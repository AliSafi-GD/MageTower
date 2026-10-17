using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Runtime-generated placeholder sprites (1 world unit at scale 1).</summary>
    public static class PlaceholderArt
    {
        /// <summary>White 1x1-unit square sprite, centre pivot.</summary>
        public static Sprite CreateSquare() => Create(4, false);

        /// <summary>White 1-unit-diameter circle sprite, centre pivot.</summary>
        public static Sprite CreateCircle() => Create(64, true);

        /// <summary>White 1-unit-diameter ring (outer radius 0.5, inner radius 0.4), centre pivot.</summary>
        public static Sprite CreateRing()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            float inner = r * 0.8f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d2 = dx * dx + dy * dy;
                    bool inside = d2 <= r * r && d2 >= inner * inner;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite Create(int size, bool circle)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = circle ? FilterMode.Bilinear : FilterMode.Point
            };
            float r = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    bool inside = !circle || dx * dx + dy * dy <= r * r;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
