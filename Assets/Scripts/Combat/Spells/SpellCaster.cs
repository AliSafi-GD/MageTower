using System.Collections.Generic;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Auto-casts unlocked spells (Story 004, design/game-brief.md MVP feature 4). One shared cast loop; the per-spell
    /// effect is a small branch. Target = nearest living enemy in the mage range. Damage goes through
    /// <see cref="SpellDamage"/> and the shared <see cref="CombatStats"/> layer. Kills use <see cref="Enemy.TakeDamage"/>,
    /// i.e. the normal OnKilled gold path; the IsAlive guard prevents double awards.
    /// </summary>
    public sealed class SpellCaster : MonoBehaviour
    {
        private CombatConfig _config;
        private Mage _mage;
        private IReadOnlyList<Enemy> _enemies;
        private SpellBook _book;
        private SpellEffects _effects;
        private CombatStats _stats;

        // Reused buffers (no per-frame allocations).
        private readonly List<Enemy> _victims = new List<Enemy>(32);
        private readonly List<Enemy> _alive = new List<Enemy>(64);
        private readonly List<Vector2> _positions = new List<Vector2>(64);
        private readonly List<int> _chain = new List<int>(16);
        private readonly List<Vector2> _points = new List<Vector2>(16);

        /// <summary>Injects dependencies.</summary>
        public void Initialize(CombatConfig config, Mage mage, IReadOnlyList<Enemy> enemies, ShopModel shop, SpellBook book, SpellEffects effects, CombatStats stats = null)
        {
            _stats = stats ?? config.BuildStats(shop, null);
            _config = config;
            _mage = mage;
            _enemies = enemies;
            _book = book;
            _effects = effects;
        }

        /// <summary>
        /// Damage of one spell hit: configured spell damage through <see cref="CombatStats.Damage"/> (shop damage
        /// upgrade x rune damage, the same layer as the basic attack). The single place spell damage is computed.
        /// </summary>
        public float SpellDamage(SpellType type) => _stats.Damage(_config.SpellDamage(type));

        private void Update()
        {
            Vector2 origin = _mage.transform.position;
            float dt = Time.deltaTime;
            for (int i = 0; i < SpellBook.SpellCount; i++)
            {
                var type = (SpellType)i;
                if (!_book.IsUnlocked(type)) continue;
                // Re-pick per spell: an earlier spell this frame may have killed the previous target.
                Enemy target = TargetSelector.FindNearest(origin, _enemies, _config.MageRange);
                if (_book.Tick(type, dt, target != null)) Cast(type, origin, target);
            }
        }

        private void Cast(SpellType type, Vector2 origin, Enemy target)
        {
            float damage = SpellDamage(type);
            switch (type)
            {
                case SpellType.Fireball:
                    CollectWithin(target.Position, _config.FireballRadius);
                    _effects.ShowFireball(target.Position, _config.FireballRadius);
                    for (int i = 0; i < _victims.Count; i++) _victims[i].TakeDamage(damage);
                    break;
                case SpellType.Frost:
                    CollectWithin(target.Position, _config.FrostRadius);
                    _effects.ShowFrost(target.Position, _config.FrostRadius);
                    float mult = SpellMath.SlowSpeedMultiplier(_config.FrostSlowFraction);
                    for (int i = 0; i < _victims.Count; i++)
                    {
                        _victims[i].ApplySlow(mult, _config.FrostSlowDuration);
                        _victims[i].TakeDamage(damage);
                    }
                    break;
                default:
                    CastLightning(origin, target, damage);
                    break;
            }
            _victims.Clear();
        }

        private void CastLightning(Vector2 origin, Enemy target, float damage)
        {
            _alive.Clear();
            _positions.Clear();
            int start = -1;
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (e == target) start = _alive.Count;
                _alive.Add(e);
                _positions.Add(e.Position);
            }
            SpellMath.SelectChain(_positions, start, _config.LightningJumpRange, _config.LightningMaxTargets, _chain);

            _points.Clear();
            _points.Add(origin);
            for (int i = 0; i < _chain.Count; i++) _points.Add(_positions[_chain[i]]);
            _effects.ShowLightning(_points);
            // Damage after the chain is fixed: kills mutate the shared enemy list but not our snapshot.
            for (int i = 0; i < _chain.Count; i++) _alive[_chain[i]].TakeDamage(damage);
        }

        private void CollectWithin(Vector2 center, float radius)
        {
            _victims.Clear();
            float sqr = radius * radius;
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if ((e.Position - center).sqrMagnitude <= sqr) _victims.Add(e);
            }
        }
    }
}
