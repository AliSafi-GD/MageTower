using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Short-lived visual: optionally rises, scales and fades out, then destroys itself.</summary>
    public sealed class FadingEffect : MonoBehaviour
    {
        private SpriteRenderer _sprite;
        private TextMesh _text;
        private Color _color;
        private Vector3 _baseScale;
        private float _lifetime;
        private float _riseSpeed;
        private float _startMul;
        private float _endMul;
        private float _age;

        /// <summary>Starts the effect using whichever of SpriteRenderer/TextMesh is on this object.</summary>
        public void Begin(float lifetime, float riseSpeed, float startMultiplier, float endMultiplier)
        {
            _sprite = GetComponent<SpriteRenderer>();
            _text = GetComponent<TextMesh>();
            _color = _sprite != null ? _sprite.color : _text.color;
            _baseScale = transform.localScale;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _riseSpeed = riseSpeed;
            _startMul = startMultiplier;
            _endMul = endMultiplier;
            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _lifetime;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            transform.Translate(0f, _riseSpeed * Time.deltaTime, 0f, Space.World);
            Apply(t);
        }

        private void Apply(float t)
        {
            transform.localScale = _baseScale * Mathf.Lerp(_startMul, _endMul, t);
            Color c = _color;
            c.a *= 1f - t * t;
            if (_sprite != null) _sprite.color = c;
            else _text.color = c;
        }
    }
}
