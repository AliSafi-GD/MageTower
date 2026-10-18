using System;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Placeholder enemy: walks left toward the tower, damages it on contact, dies when HP hits 0.</summary>
    public sealed class Enemy : MonoBehaviour, ITargetable
    {
        /// <summary>Raised on every hit: (enemy, damage, wasCrit).</summary>
        public event Action<Enemy, float, bool> OnDamaged;
        /// <summary>Raised once when the enemy is killed by damage (not when it reaches the tower), before <see cref="OnRemoved"/>.</summary>
        public event Action<Enemy> OnKilled;
        /// <summary>Raised once when the enemy is removed for any reason (killed or reached tower).</summary>
        public event Action<Enemy> OnRemoved;

        private CombatConfig _config;
        private Tower _tower;
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private float _hp;
        private float _contactDamage;
        private float _size;
        private float _flashTimer;
        private float _slowTimer;
        private float _slowMultiplier = 1f;
        private bool _removed;

        /// <inheritdoc />
        public bool IsAlive => !_removed && _hp > 0f;
        /// <inheritdoc />
        public Vector2 Position => transform.position;

        /// <summary>True while a frost slow is active.</summary>
        public bool IsSlowed => _slowTimer > 0f;

        /// <summary>Current walk-speed multiplier (1 = normal; below 1 while slowed).</summary>
        public float SpeedMultiplier => _slowTimer > 0f ? _slowMultiplier : 1f;

        /// <summary>True for boss enemies.</summary>
        public bool IsBoss { get; private set; }

        /// <summary>Gold awarded to the player if this enemy is killed (set by the spawner).</summary>
        public double GoldReward { get; set; }

        /// <summary>Permanent run currency awarded if this enemy is killed (set by the spawner). Story 006.</summary>
        public double MetaReward { get; set; }

        /// <summary>Sets up per-instance stats (from <see cref="WaveScaling"/>) and body size.</summary>
        public void Initialize(CombatConfig config, Tower tower, float hp, float contactDamage, float size, bool isBoss)
        {
            _config = config;
            _tower = tower;
            _hp = hp;
            _contactDamage = contactDamage;
            _size = size;
            IsBoss = isBoss;
            _renderer = GetComponent<SpriteRenderer>();
            _baseColor = _renderer.color;
            _baseScale = transform.localScale;
        }

        /// <summary>Applies damage; triggers hit flash/punch and removes the enemy on death.</summary>
        public void TakeDamage(float amount, bool isCrit = false)
        {
            if (!IsAlive) return;
            _hp -= amount;
            _flashTimer = _config.HitFlashDuration;
            OnDamaged?.Invoke(this, amount, isCrit);
            if (_hp <= 0f)
            {
                OnKilled?.Invoke(this);
                Remove();
            }
        }

        /// <summary>
        /// Slows the enemy to <paramref name="speedMultiplier"/> of normal speed for <paramref name="duration"/> seconds.
        /// Refreshes rather than stacks: a new slow replaces the multiplier and restarts the timer. Story 004 (frost).
        /// </summary>
        public void ApplySlow(float speedMultiplier, float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            _slowMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
            _slowTimer = duration;
        }

        private void Update()
        {
            if (_removed) return;
            float dt = Time.deltaTime;
            transform.Translate(Vector3.left * (_config.EnemyMoveSpeed * SpeedMultiplier * dt), Space.World);

            // Resting colour: base, or the frost tint (alpha = strength) while slowed. Expires back to normal.
            Color rest = _baseColor;
            if (_slowTimer > 0f)
            {
                _slowTimer = Mathf.Max(0f, _slowTimer - dt);
                if (_slowTimer > 0f)
                {
                    Color tint = _config.FrostTintColor;
                    rest = Color.Lerp(_baseColor, new Color(tint.r, tint.g, tint.b, _baseColor.a), tint.a);
                }
            }

            if (_flashTimer > 0f)
            {
                _flashTimer = Mathf.Max(0f, _flashTimer - dt);
                float t = _flashTimer / _config.HitFlashDuration;
                _renderer.color = Color.Lerp(rest, _config.HitFlashColor, t);
                transform.localScale = _baseScale * (1f + _config.HitPunchScale * t);
            }
            else
            {
                _renderer.color = rest;
            }

            if (transform.position.x - _size * 0.5f <= _tower.ContactX)
            {
                _tower.TakeDamage(_contactDamage);
                Remove();
            }
        }

        private void OnDestroy()
        {
            // Destroyed externally (e.g. scene unload): still notify so listeners drop the reference.
            if (_removed) return;
            _removed = true;
            OnRemoved?.Invoke(this);
        }

        private void Remove()
        {
            if (_removed) return;
            _removed = true;
            OnRemoved?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
