using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// What <see cref="RunLifecycle"/> needs back from <see cref="CombatBootstrap.BuildRun"/>: enough to know when
    /// the run ends and what to tear down before building the next one. Story 006.
    /// </summary>
    public readonly struct RunHandles
    {
        /// <summary>Root transform of every GameObject this run created; destroying it tears the run down.</summary>
        public readonly Transform Root;
        /// <summary>This run's tower; fires <see cref="Tower.OnDestroyed"/> once, when HP reaches 0.</summary>
        public readonly Tower Tower;
        /// <summary>This run's wave state, read for the run-summary screen.</summary>
        public readonly WaveModel Waves;

        /// <summary>Creates the handle set.</summary>
        public RunHandles(Transform root, Tower tower, WaveModel waves)
        {
            Root = root;
            Tower = tower;
            Waves = waves;
        }
    }
}
