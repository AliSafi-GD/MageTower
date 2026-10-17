using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Auto-attacking mage: targets the nearest living enemy in range, idles with none.
    /// Implements Story 001 (design/game-brief.md MVP feature 1).
    /// </summary>
    public sealed class Mage : MonoBehaviour
    {
        /// <summary>Raised on each attack: (mage position, target position).</summary>
        public event Action<Vector2, Vector2> OnAttackFired;

        private CombatConfig _config;
        private IReadOnlyList<Enemy> _enemies;
        private ShopModel _shop;
        private CombatStats _stats;
        private Func<float> _roll;
        private AttackCooldown _cooldown;
        private Vector3 _baseScale;
        private float _punchTimer;

        /// <summary>Current target, or null while idle.</summary>
        public Enemy CurrentTarget { get; private set; }

        /// <summary>Injects config and the live enemy list.</summary>
        /// <param name="shop">Upgrade levels read on every attack (damage, attack speed, crit); may be null for no upgrades.</param>
        /// <param name="stats">The shared stat-modifier layer (shop + runes); if null, one is built from <paramref name="shop"/> alone.</param>
        /// <param name="roll">Uniform [0,1) random source for crits; defaults to UnityEngine.Random.value. Inject for deterministic tests.</param>
        public void Initialize(CombatConfig config, IReadOnlyList<Enemy> enemies, ShopModel shop = null, Func<float> roll = null, CombatStats stats = null)
        {
            _config = config;
            _stats = stats ?? config.BuildStats(shop, null);
            _enemies = enemies;
            _shop = shop;
            _roll = roll ?? (() => UnityEngine.Random.value);
            _cooldown = new AttackCooldown(config.MageAttacksPerSecond, config.MaxAttacksPerSecond);
            _baseScale = transform.localScale;
        }

        private int Level(UpgradeType t) => _shop != null ? _shop.GetLevel(t) : 0;

        /// <summary>Non-crit damage per attack, through <see cref="CombatStats.Damage"/> (shop x runes). Crit is applied after.</summary>
        public float BaseDamage => _stats.Damage(_config.MageDamage);
        /// <summary>Attacks per second through <see cref="CombatStats.AttacksPerSecond"/> (shop attack speed, rune cooldown, clamped).</summary>
        public float CurrentAttacksPerSecond => _stats.AttacksPerSecond(_config.MageAttacksPerSecond);
        /// <summary>Crit chance including the crit upgrade (capped).</summary>
        public float CritChance => UpgradeEffects.CritChance(_config.BaseCritChance, _config.CritChancePerLevel, Level(UpgradeType.CritChance), _config.MaxCritChance);

        private void OnDisable()
        {
            // Avoid freezing at punch scale if disabled mid-punch (e.g. when the tower falls).
            if (_config == null) return;
            _punchTimer = 0f;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _cooldown.SetAttacksPerSecond(CurrentAttacksPerSecond);
            Vector2 origin = transform.position;
            CurrentTarget = TargetSelector.FindNearest(origin, _enemies, _config.MageRange);

            if (_cooldown.Tick(dt, CurrentTarget != null))
            {
                _punchTimer = _config.AttackPunchDuration;
                OnAttackFired?.Invoke(origin, CurrentTarget.Position);
                bool crit = UpgradeEffects.IsCrit(CritChance, _roll());
                float damage = BaseDamage * (crit ? _config.CritDamageMultiplier : 1f);
                CurrentTarget.TakeDamage(damage, crit);
            }

            if (_punchTimer > 0f)
            {
                _punchTimer = Mathf.Max(0f, _punchTimer - dt);
                transform.localScale = _baseScale * (1f + _config.AttackPunchScale * (_punchTimer / _config.AttackPunchDuration));
            }
        }
    }
}
