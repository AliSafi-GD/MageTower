using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// All permanent (cross-run) state in one plain C# object (no UnityEngine dependency): the run currency
    /// (<see cref="MetaWallet"/>), the permanent <see cref="SkillTree"/> and the best-wave record. Story 006
    /// (design/game-brief.md MVP feature 6). Owned by <see cref="RunLifecycle"/> and never parented to a run, so a new
    /// run cannot reset it; Story 007 saves/loads this one object. Holds no reference to any run object: the run side
    /// pushes into it (<see cref="RecordWave"/>, <see cref="MetaWallet.Add"/>), never the other way round.
    /// </summary>
    public sealed class MetaProgress
    {
        /// <summary>Permanent run-currency balance.</summary>
        public MetaWallet Wallet { get; }
        /// <summary>Permanent skill tree.</summary>
        public SkillTree Tree { get; }
        /// <summary>Highest wave ever reached (0 before any wave).</summary>
        public int BestWave { get; private set; }

        /// <summary>Creates meta state from an existing wallet and tree (e.g. loaded by Story 007).</summary>
        public MetaProgress(MetaWallet wallet, SkillTree tree, int bestWave = 0)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            BestWave = Math.Max(0, bestWave);
        }

        /// <summary>Raises the best-wave record if <paramref name="wave"/> beats it; returns whether it did.</summary>
        public bool RecordWave(int wave)
        {
            if (wave <= BestWave) return false;
            BestWave = wave;
            return true;
        }
    }
}
