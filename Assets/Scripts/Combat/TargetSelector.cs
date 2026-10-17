using System.Collections.Generic;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Pure targeting logic: nearest living candidate within range.</summary>
    public static class TargetSelector
    {
        /// <summary>
        /// Returns the nearest living candidate within <paramref name="range"/> of <paramref name="origin"/>, or null.
        /// </summary>
        public static T FindNearest<T>(Vector2 origin, IReadOnlyList<T> candidates, float range) where T : class, ITargetable
        {
            T best = null;
            float bestSqr = range * range;
            for (int i = 0; i < candidates.Count; i++)
            {
                T c = candidates[i];
                if (c == null || !c.IsAlive) continue;
                float sqr = (c.Position - origin).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = c;
                }
            }
            return best;
        }
    }
}
