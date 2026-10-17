using System;
using System.Globalization;

namespace ArcaneSpire.Combat
{
    /// <summary>Pure short-number formatting (1.2K, 3.4M, ...). Story 003 acceptance criterion 7.</summary>
    public static class NumberFormat
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

        /// <summary>
        /// Formats a value: below 999.5 as a whole number, from 999.5 upward with one decimal and a suffix
        /// (trailing ".0" dropped). Rounding is away from zero. Negatives keep their sign, but a value that
        /// rounds to zero is shown as "0" (never "-0"). NaN gives "0", infinity gives "inf"/"-inf".
        /// </summary>
        public static string Short(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return value > 0 ? "inf" : "-inf";
            bool negative = value < 0;
            string body = FormatPositive(Math.Abs(value));
            return negative && body != "0" ? "-" + body : body;
        }

        private static string FormatPositive(double value)
        {
            if (value < 999.5)
                return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

            int tier = 0;
            double scaled = value;
            while (scaled >= 1000.0 && tier < Suffixes.Length - 1)
            {
                scaled /= 1000.0;
                tier++;
            }
            // 999.5..999.999 skipped the loop but rounds to 1000: show it as 1K.
            if (tier == 0)
            {
                scaled /= 1000.0;
                tier = 1;
            }
            scaled = Math.Round(scaled, 1, MidpointRounding.AwayFromZero);
            // Rounding to one decimal may reach 1000 (e.g. 999,960 -> 1000K): promote to the next suffix.
            if (scaled >= 1000.0 && tier < Suffixes.Length - 1)
            {
                scaled = Math.Round(scaled / 1000.0, 1, MidpointRounding.AwayFromZero);
                tier++;
            }
            return scaled.ToString("0.#", CultureInfo.InvariantCulture) + Suffixes[tier];
        }
    }
}
