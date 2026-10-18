using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Plain C# wave state: current wave and best wave reached this session (not persisted; saving is Story 007).
    /// Implements design/game-brief.md MVP feature 2.
    /// </summary>
    public sealed class WaveModel
    {
        /// <summary>Raised when the wave changes: (currentWave, bestWave).</summary>
        public event Action<int, int> OnWaveChanged;

        /// <summary>Current wave number; 0 before the first wave starts.</summary>
        public int CurrentWave { get; private set; }

        /// <summary>Highest wave reached so far.</summary>
        public int BestWave { get; private set; }

        /// <summary>
        /// Creates wave state. <paramref name="bestWave"/> seeds the best-wave record (Story 006: a new run starts
        /// at wave 1 but keeps the best-wave record from earlier runs, so the record survives the fresh
        /// <see cref="WaveModel"/> instance each run builds).
        /// </summary>
        public WaveModel(int bestWave = 0)
        {
            BestWave = Math.Max(0, bestWave);
        }

        /// <summary>Advances to the next wave and updates the best-wave record.</summary>
        public void StartNextWave()
        {
            CurrentWave++;
            if (CurrentWave > BestWave) BestWave = CurrentWave;
            OnWaveChanged?.Invoke(CurrentWave, BestWave);
        }
    }
}
