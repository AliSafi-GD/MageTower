using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Pure, deterministic wave -> enemy stats functions (no Unity dependency, no randomness).
    /// Implements design/game-brief.md MVP feature 2. All constants come from <see cref="CombatConfig"/>.
    /// Wave numbers start at 1; values below 1 are treated as 1.
    /// </summary>
    public static class WaveScaling
    {
        /// <summary>Upper bound for any scaled HP or damage value, so very high waves stay finite and killable.</summary>
        public const float MaxStatValue = 1e7f;

        /// <summary>Regular (non-boss) enemy HP for the wave: base * (1 + growth)^(wave-1), capped at <see cref="MaxStatValue"/>.</summary>
        public static float EnemyHp(CombatConfig c, int wave) =>
            Cap(c.EnemyMaxHp * Growth(c.EnemyHpGrowthPerWave, wave));

        /// <summary>Enemy contact damage for the wave: base * (1 + growth)^(wave-1), capped at <see cref="MaxStatValue"/>.</summary>
        public static float EnemyContactDamage(CombatConfig c, int wave) =>
            Cap(c.EnemyContactDamage * Growth(c.EnemyDamageGrowthPerWave, wave));

        /// <summary>Boss HP for the wave: regular HP * boss multiplier, capped at <see cref="MaxStatValue"/>.</summary>
        public static float BossHp(CombatConfig c, int wave) => Cap(EnemyHp(c, wave) * c.BossHpMultiplier);

        /// <summary>
        /// Gold for a kill: round(goldPerKill * (1 + growth)^(wave-1)) (at least 1 if goldPerKill &gt; 0),
        /// times the boss multiple for bosses.
        /// </summary>
        public static double KillGold(CombatConfig c, int wave, bool isBoss)
        {
            if (c.GoldPerKill <= 0f) return 0.0;
            // Computed in double (not the float Growth helper) so large waves keep precision.
            double growth = Math.Pow(1.0 + (double)c.GoldGrowthPerWave, Math.Max(1, wave) - 1);
            double gold = Math.Max(1.0, Math.Round((double)c.GoldPerKill * growth));
            if (double.IsInfinity(gold)) gold = double.MaxValue;
            return isBoss ? Math.Round(gold * c.BossGoldMultiplier) : gold;
        }

        /// <summary>
        /// Permanent run currency for a kill (Story 006, design/game-brief.md MVP feature 6): round(runCurrencyPerKill
        /// * (1 + growth)^(wave-1)) (at least 1 if runCurrencyPerKill &gt; 0), times the boss multiple for bosses.
        /// Mirrors <see cref="KillGold"/> exactly but reads the separate run-currency config fields, so the meta
        /// economy can be tuned independently of the in-run gold economy.
        /// </summary>
        public static double RunCurrencyReward(CombatConfig c, int wave, bool isBoss)
        {
            if (c.RunCurrencyPerKill <= 0f) return 0.0;
            double growth = Math.Pow(1.0 + (double)c.RunCurrencyGrowthPerWave, Math.Max(1, wave) - 1);
            double amount = Math.Max(1.0, Math.Round((double)c.RunCurrencyPerKill * growth));
            if (double.IsInfinity(amount)) amount = double.MaxValue;
            return isBoss ? Math.Round(amount * c.RunCurrencyBossMultiplier) : amount;
        }

        private static float Cap(float value) => Math.Min(value, MaxStatValue);

        /// <summary>Enemies in the wave (boss included): base + floor((wave-1)*growth), capped at the max.</summary>
        public static int EnemyCount(CombatConfig c, int wave)
        {
            int w = Math.Max(1, wave);
            int count = c.BaseEnemyCount + (int)Math.Floor((w - 1) * (double)c.EnemyCountGrowthPerWave);
            return Math.Max(1, Math.Min(count, Math.Max(1, c.MaxEnemyCount)));
        }

        /// <summary>True when the wave is a multiple of BossEveryNWaves (0 disables bosses).</summary>
        public static bool IsBossWave(CombatConfig c, int wave) =>
            c.BossEveryNWaves > 0 && wave >= 1 && wave % c.BossEveryNWaves == 0;

        private static float Growth(float rate, int wave) =>
            (float)Math.Pow(1.0 + rate, Math.Max(1, wave) - 1);
    }
}
