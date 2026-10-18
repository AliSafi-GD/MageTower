using System;
using System.Globalization;

namespace ArcaneSpire.Combat
{
    /// <summary>Outcome of an offline-gain estimate.</summary>
    public readonly struct OfflineResult
    {
        /// <summary>Real time away in seconds (never negative).</summary>
        public readonly double ElapsedSeconds;
        /// <summary>Seconds that count toward gains (elapsed, capped).</summary>
        public readonly double CreditedSeconds;
        /// <summary>True if the absence exceeded the cap.</summary>
        public readonly bool Capped;
        /// <summary>Run currency earned while away (whole number, 0 if away less than the minimum).</summary>
        public readonly double Currency;

        /// <summary>Creates a result.</summary>
        public OfflineResult(double elapsedSeconds, double creditedSeconds, bool capped, double currency)
        {
            ElapsedSeconds = elapsedSeconds;
            CreditedSeconds = creditedSeconds;
            Capped = capped;
            Currency = currency;
        }
    }

    /// <summary>
    /// Pure offline-progress estimate (Story 007, design/game-brief.md MVP feature 7). NOT a simulation: a closed-form
    /// estimate from the best-wave power curve.
    ///   waveOffline   = max(1, floor(bestWave * OfflineWaveFraction))
    ///   perKill       = RunCurrencyReward(waveOffline, regular) + (boss - regular) / (BossEveryNWaves * EnemyCount(waveOffline))
    ///                   (one boss kill per BossEveryNWaves waves, averaged over the kills of those waves)
    ///   currency/hour = OfflineKillsPerHour * OfflineEfficiency * perKill   (0 when bestWave &lt;= 0)
    ///   gain          = floor(currency/hour * min(elapsed, OfflineMaxHours*3600) / 3600), 0 if elapsed &lt; OfflineMinAwaySeconds
    /// All constants live in <see cref="CombatConfig"/>.
    /// </summary>
    public static class OfflineProgress
    {
        /// <summary>Added before flooring so float-to-double representation error cannot drop a whole unit.</summary>
        public const double FloorEpsilon = 1e-6;

        /// <summary>Estimated run currency per hour for the given best wave.</summary>
        public static double CurrencyPerHour(CombatConfig c, int bestWave)
        {
            if (c == null || bestWave <= 0) return 0.0;
            // The float fraction widened to double is slightly off (0.6f -> 0.60000002384...); the epsilon keeps
            // e.g. 10 * 0.6 from flooring to 5 if the representation lands just below the integer.
            int wave = Math.Max(1, (int)Math.Floor(bestWave * (double)c.OfflineWaveFraction + FloorEpsilon));
            double regular = WaveScaling.RunCurrencyReward(c, wave, false);
            double perKill = regular;
            int bossEvery = c.BossEveryNWaves;
            if (bossEvery > 0)
            {
                double boss = WaveScaling.RunCurrencyReward(c, wave, true);
                perKill += Math.Max(0.0, boss - regular) / (bossEvery * (double)WaveScaling.EnemyCount(c, wave));
            }
            double perHour = c.OfflineKillsPerHour * (double)c.OfflineEfficiency * perKill;
            return double.IsNaN(perHour) || double.IsInfinity(perHour) ? 0.0 : Math.Max(0.0, perHour);
        }

        /// <summary>Sums two results (for a popup that is still open when a second absence is credited).</summary>
        public static OfflineResult Combine(OfflineResult a, OfflineResult b) =>
            new OfflineResult(a.ElapsedSeconds + b.ElapsedSeconds, a.CreditedSeconds + b.CreditedSeconds,
                a.Capped || b.Capped, a.Currency + b.Currency);

        /// <summary>
        /// Estimates the gain for an absence. Negative or NaN elapsed time (clock moved backwards) counts as 0.
        /// </summary>
        public static OfflineResult Compute(CombatConfig c, int bestWave, double elapsedSeconds)
        {
            double elapsed = double.IsNaN(elapsedSeconds) || elapsedSeconds < 0.0 ? 0.0 : elapsedSeconds;
            if (double.IsInfinity(elapsed)) elapsed = double.MaxValue;
            double cap = c.OfflineMaxHours * 3600.0;
            double credited = Math.Min(elapsed, cap);
            bool capped = elapsed > cap;
            double currency = 0.0;
            if (elapsed >= c.OfflineMinAwaySeconds)
                currency = Math.Floor(CurrencyPerHour(c, bestWave) * credited / 3600.0 + FloorEpsilon);
            return new OfflineResult(elapsed, credited, capped, currency);
        }

        /// <summary>Formats seconds as "Xh Ym", "Ym Zs" or "Zs".</summary>
        public static string FormatDuration(double seconds)
        {
            long s = (long)Math.Max(0.0, Math.Min(seconds, 1e12));
            long h = s / 3600, m = s % 3600 / 60, sec = s % 60;
            if (h > 0) return string.Format(CultureInfo.InvariantCulture, "{0}h {1}m", h, m);
            if (m > 0) return string.Format(CultureInfo.InvariantCulture, "{0}m {1}s", m, sec);
            return string.Format(CultureInfo.InvariantCulture, "{0}s", sec);
        }
    }
}
