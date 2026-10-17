using System.Collections.Generic;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Pure spell helpers (no scene access): lightning chain selection and slow math. Story 004.</summary>
    public static class SpellMath
    {
        /// <summary>
        /// Selects the lightning chain: starts at <paramref name="startIndex"/>, then repeatedly jumps to the nearest
        /// not-yet-hit position within <paramref name="jumpRange"/> of the LAST hit position, until
        /// <paramref name="maxCount"/> indices are chosen or no candidate is in range. Indices are distinct
        /// (no position is hit twice). <paramref name="result"/> is cleared and filled in hit order (reusable buffer).
        /// </summary>
        public static void SelectChain(IReadOnlyList<Vector2> positions, int startIndex, float jumpRange, int maxCount, List<int> result)
        {
            result.Clear();
            if (positions == null || maxCount <= 0 || startIndex < 0 || startIndex >= positions.Count) return;
            result.Add(startIndex);
            float rangeSqr = jumpRange * jumpRange;
            while (result.Count < maxCount)
            {
                Vector2 from = positions[result[result.Count - 1]];
                int best = -1;
                float bestSqr = rangeSqr;
                for (int i = 0; i < positions.Count; i++)
                {
                    if (result.Contains(i)) continue;
                    float sqr = (positions[i] - from).sqrMagnitude;
                    if (sqr <= bestSqr)
                    {
                        bestSqr = sqr;
                        best = i;
                    }
                }
                if (best < 0) break;
                result.Add(best);
            }
        }

        /// <summary>Enemy speed multiplier for a slow of <paramref name="slowFraction"/> (0.5 = 50% slower), clamped to [0.05, 1].</summary>
        public static float SlowSpeedMultiplier(float slowFraction) =>
            float.IsNaN(slowFraction) ? 1f : Mathf.Clamp(1f - slowFraction, 0.05f, 1f);
    }
}
