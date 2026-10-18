using System;
using System.Collections.Generic;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// All permanent (cross-run) state in one plain C# object (no UnityEngine dependency): the run currency
    /// (<see cref="MetaWallet"/>), the permanent <see cref="SkillTree"/> and the best-wave record. Story 006
    /// (design/game-brief.md MVP feature 6). Owned by <see cref="RunLifecycle"/> and never parented to a run, so a new
    /// run cannot reset it; Story 007 saves/loads this one object (<see cref="Restore"/>, <see cref="MetaSaveMapper"/>).
    /// Holds no reference to any run object: the run side pushes into it (<see cref="RecordWave"/>,
    /// <see cref="MetaWallet.Add"/>), never the other way round.
    /// </summary>
    public sealed class MetaProgress
    {
        private readonly List<RuneType> _lastRuneLoadout = new List<RuneType>();

        /// <summary>Permanent run-currency balance.</summary>
        public MetaWallet Wallet { get; }
        /// <summary>Permanent skill tree.</summary>
        public SkillTree Tree { get; }
        /// <summary>Highest wave ever reached (0 before any wave).</summary>
        public int BestWave { get; private set; }

        /// <summary>
        /// The runes the player had equipped when their last run ended (Story 007). Stored and saved only: runes are
        /// run state, so this is NOT applied to a new run (no auto-unlock or auto-equip).
        /// </summary>
        public IReadOnlyList<RuneType> LastRuneLoadout => _lastRuneLoadout;

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

        /// <summary>
        /// Records the rune loadout of the run that just ended. Duplicates and undefined values are dropped; order is kept.
        /// </summary>
        public void SetLastRuneLoadout(IEnumerable<RuneType> loadout)
        {
            _lastRuneLoadout.Clear();
            if (loadout == null) return;
            foreach (RuneType r in loadout)
                if (Enum.IsDefined(typeof(RuneType), r) && !_lastRuneLoadout.Contains(r)) _lastRuneLoadout.Add(r);
        }

        /// <summary>
        /// Story 007 restore (TD-022): replaces currency, skill levels, best wave and last rune loadout in one step.
        /// All inputs are validated first; if any is invalid nothing is changed and false is returned. Raises the
        /// wallet/tree change events so views refresh.
        /// </summary>
        public bool Restore(double currency, int[] skillLevels, int bestWave, IEnumerable<RuneType> loadout)
        {
            if (double.IsNaN(currency) || double.IsInfinity(currency) || currency < 0.0) return false;
            if (bestWave < 0) return false;
            if (skillLevels == null || skillLevels.Length != SkillTree.NodeCount) return false;
            for (int i = 0; i < skillLevels.Length; i++)
                if (skillLevels[i] < 0 || skillLevels[i] > Tree.MaxLevel) return false;

            Tree.SetLevels(skillLevels);
            Wallet.Set(currency);
            BestWave = bestWave;
            SetLastRuneLoadout(loadout);
            return true;
        }
    }
}
