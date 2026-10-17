using System;
using System.Collections.Generic;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Extra multiplicative stat source (e.g. Story 006 permanent upgrades). Each factor is 1 when the source has no effect.
    /// </summary>
    public interface IStatModifierSource
    {
        /// <summary>Damage factor (basic attack and spells).</summary>
        float DamageMultiplier { get; }
        /// <summary>Attack-rate factor of the basic attack.</summary>
        float AttackSpeedMultiplier { get; }
        /// <summary>Cooldown factor (spell cooldowns and basic attack interval); lower = faster.</summary>
        float CooldownMultiplier { get; }
        /// <summary>Kill gold factor.</summary>
        float GoldMultiplier { get; }
    }

    /// <summary>
    /// The single stat-modifier layer (Story 005 implementation note: "route all stat changes through one layer shared
    /// with shop upgrades and spells"). Plain C# (no UnityEngine). Combines, each exactly once:
    /// shop upgrades (<see cref="ShopModel"/> levels), equipped runes (<see cref="RuneBook"/>) and any extra
    /// <see cref="IStatModifierSource"/> (room for Story 006). Sources multiply with each other; within the rune
    /// layer the same modifier type adds (see <see cref="RuneEffects.Combine"/>). Everything is evaluated on read,
    /// so a change in any source applies immediately and removing it restores the previous value exactly.
    /// Crit is applied by the caller after <see cref="Damage"/>.
    /// </summary>
    public sealed class CombatStats
    {
        private readonly ShopModel _shop;
        private readonly RuneBook _runes;
        private readonly float _damageBonusPerLevel;
        private readonly float _attackSpeedBonusPerLevel;
        private readonly float _maxAttacksPerSecond;
        private readonly float _minCooldownMultiplier;
        private readonly Func<SpellType, float> _baseSpellCooldown;
        private readonly List<IStatModifierSource> _extra = new List<IStatModifierSource>(2);

        /// <summary>
        /// Creates the layer. <paramref name="shop"/> and <paramref name="runes"/> may be null (no effect).
        /// <paramref name="baseSpellCooldown"/> maps a spell to its configured cooldown in seconds.
        /// </summary>
        public CombatStats(ShopModel shop, RuneBook runes, float damageBonusPerLevel, float attackSpeedBonusPerLevel,
            float maxAttacksPerSecond, float minCooldownMultiplier, Func<SpellType, float> baseSpellCooldown)
        {
            _shop = shop;
            _runes = runes;
            _damageBonusPerLevel = damageBonusPerLevel;
            _attackSpeedBonusPerLevel = attackSpeedBonusPerLevel;
            _maxAttacksPerSecond = maxAttacksPerSecond > 0f ? maxAttacksPerSecond : AttackCooldown.DefaultMaxAttacksPerSecond;
            _minCooldownMultiplier = float.IsNaN(minCooldownMultiplier) ? 0.05f : Math.Max(0.01f, minCooldownMultiplier);
            _baseSpellCooldown = baseSpellCooldown ?? throw new ArgumentNullException(nameof(baseSpellCooldown));
        }

        /// <summary>Adds an extra multiplicative source (e.g. Story 006 permanent upgrades).</summary>
        public void AddSource(IStatModifierSource source)
        {
            if (source != null && !_extra.Contains(source)) _extra.Add(source);
        }

        private int Level(UpgradeType t) => _shop != null ? _shop.GetLevel(t) : 0;

        /// <summary>Total damage factor: shop damage upgrade x rune damage x extra sources.</summary>
        public float DamageMultiplier
        {
            get
            {
                float m = UpgradeEffects.DamageMultiplier(_damageBonusPerLevel, Level(UpgradeType.Damage));
                if (_runes != null) m *= _runes.DamageMultiplier;
                for (int i = 0; i < _extra.Count; i++) m *= _extra[i].DamageMultiplier;
                return m;
            }
        }

        /// <summary>Total cooldown factor: rune cooldown x extra sources, floored so cooldowns never reach 0.</summary>
        public float CooldownMultiplier
        {
            get
            {
                float m = _runes != null ? _runes.CooldownMultiplier : 1f;
                for (int i = 0; i < _extra.Count; i++) m *= _extra[i].CooldownMultiplier;
                return Math.Max(_minCooldownMultiplier, m);
            }
        }

        /// <summary>Total kill gold factor: rune gold x extra sources.</summary>
        public float GoldMultiplier
        {
            get
            {
                float m = _runes != null ? _runes.GoldMultiplier : 1f;
                for (int i = 0; i < _extra.Count; i++) m *= _extra[i].GoldMultiplier;
                return Math.Max(0f, m);
            }
        }

        /// <summary>Non-crit damage of a hit with the given base damage (basic attack or spell).</summary>
        public float Damage(float baseDamage) => baseDamage * DamageMultiplier;

        /// <summary>
        /// Attacks per second: base x shop attack-speed upgrade x extra attack-speed factors / cooldown factor,
        /// clamped to the configured maximum.
        /// </summary>
        public float AttacksPerSecond(float baseAttacksPerSecond)
        {
            float m = UpgradeEffects.AttackSpeedMultiplier(_attackSpeedBonusPerLevel, Level(UpgradeType.AttackSpeed));
            for (int i = 0; i < _extra.Count; i++) m *= _extra[i].AttackSpeedMultiplier;
            return Math.Min(_maxAttacksPerSecond, baseAttacksPerSecond * m / CooldownMultiplier);
        }

        /// <summary>Spell cooldown in seconds: configured cooldown x cooldown factor.</summary>
        public float SpellCooldown(SpellType type) => _baseSpellCooldown(type) * CooldownMultiplier;

        /// <summary>Kill gold after the gold factor, in whole gold (see <see cref="RuneEffects.ApplyGold"/>).</summary>
        public double ApplyGold(double reward) => RuneEffects.ApplyGold(reward, GoldMultiplier);
    }
}
