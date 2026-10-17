using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Pure functions: upgrade level -> cost / effect values. No Unity dependency, no randomness
    /// (the crit roll is passed in). All constants are supplied from <see cref="CombatConfig"/>.
    /// </summary>
    public static class UpgradeEffects
    {
        /// <summary>Cost of buying the next level: round(baseCost * growth^level), at least 1.</summary>
        public static double Cost(double baseCost, double growth, int level) =>
            Math.Max(1.0, Math.Round(baseCost * Math.Pow(Math.Max(1.0, growth), Math.Max(0, level))));

        /// <summary>Damage multiplier: 1 + perLevel * level.</summary>
        public static float DamageMultiplier(float perLevel, int level) => 1f + perLevel * Math.Max(0, level);

        /// <summary>Attack-rate multiplier: 1 + perLevel * level.</summary>
        public static float AttackSpeedMultiplier(float perLevel, int level) => 1f + perLevel * Math.Max(0, level);

        /// <summary>Extra tower max HP: perLevel * level.</summary>
        public static float ExtraTowerHp(float perLevel, int level) => perLevel * Math.Max(0, level);

        /// <summary>Crit chance: baseChance + perLevel * level, clamped to [0, maxChance] (maxChance from config).</summary>
        public static float CritChance(float baseChance, float perLevel, int level, float maxChance) =>
            (float)Math.Min(Math.Max(0.0, maxChance), Math.Max(0.0, baseChance + perLevel * (double)Math.Max(0, level)));

        /// <summary>True when buying another crit level would not raise the chance (cap reached, or no per-level gain).</summary>
        public static bool IsCritMaxed(float baseChance, float perLevel, int level, float maxChance) =>
            CritChance(baseChance, perLevel, level + 1, maxChance) <= CritChance(baseChance, perLevel, level, maxChance) + 1e-6f;

        /// <summary>True if a uniform roll in [0,1) is below the crit chance.</summary>
        public static bool IsCrit(float chance, float roll01) => roll01 < chance;
    }
}
