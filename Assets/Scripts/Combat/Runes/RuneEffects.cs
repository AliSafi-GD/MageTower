using System;
using System.Collections.Generic;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Pure stacking math for rune modifiers (no UnityEngine dependency). Story 005.
    /// Rule: values of the SAME modifier type add (+0.2 and +0.2 = +0.4); the per-type factors then MULTIPLY
    /// across types. Damage and Gold factors are 1 + sum; the Cooldown factor is 1 - sum, floored at a minimum
    /// so a cooldown can never reach 0. The combined factor handed to each stat is then multiplied with other
    /// sources (shop upgrades) by the consumer.
    /// </summary>
    public static class RuneEffects
    {
        /// <summary>
        /// Computes the combined factor of each modifier type from scratch. Sums run in list order, so the same
        /// set always yields the identical result (no incremental drift).
        /// </summary>
        /// <param name="active">Modifiers of the equipped runes.</param>
        /// <param name="minCooldownMultiplier">Floor of the cooldown factor (0 &lt; value &lt;= 1).</param>
        /// <param name="damage">Damage factor (1 = none).</param>
        /// <param name="gold">Gold factor (1 = none).</param>
        /// <param name="cooldown">Cooldown factor (1 = none, lower = faster).</param>
        public static void Combine(IReadOnlyList<RuneModifier> active, float minCooldownMultiplier,
            out float damage, out float gold, out float cooldown)
        {
            float d = 0f, g = 0f, c = 0f;
            if (active != null)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    switch (active[i].Type)
                    {
                        case RuneModifierType.Damage: d += active[i].Value; break;
                        case RuneModifierType.Gold: g += active[i].Value; break;
                        default: c += active[i].Value; break;
                    }
                }
            }
            float min = float.IsNaN(minCooldownMultiplier) ? 0.05f : Math.Max(0.01f, minCooldownMultiplier);
            damage = Math.Max(0f, 1f + d);
            gold = Math.Max(0f, 1f + g);
            cooldown = Math.Max(min, 1f - c);
        }

        /// <summary>Kill gold after the gold factor, rounded to whole gold (a positive reward never rounds to 0).</summary>
        public static double ApplyGold(double reward, double goldMultiplier)
        {
            if (!(reward > 0.0)) return 0.0;
            double scaled = Math.Round(reward * Math.Max(0.0, goldMultiplier), MidpointRounding.AwayFromZero);
            return Math.Max(1.0, scaled);
        }
    }
}
