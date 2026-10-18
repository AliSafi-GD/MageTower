using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Plain C# permanent skill-tree state (no UnityEngine dependency): a level per <see cref="SkillNodeType"/>
    /// and purchasing against a <see cref="MetaWallet"/>. Story 006 (design/game-brief.md MVP feature 6). Nodes are
    /// leveled (max level and cost growth from config). Implements
    /// <see cref="CombatStats.IStatModifierSource"/> directly, so a single <c>stats.AddSource(skillTree)</c> call in
    /// <see cref="CombatBootstrap.BuildRun"/> is all that is needed for every node with at least one level (except
    /// <see cref="SkillNodeType.TowerBastion"/>, applied separately — see its doc comment) to apply automatically,
    /// from the first frame of every run, including runs rebuilt after a reset. Survives run resets: callers keep
    /// the same instance across <see cref="CombatBootstrap.BuildRun"/> calls instead of recreating it.
    /// </summary>
    public sealed class SkillTree : IStatModifierSource
    {
        /// <summary>Number of skill nodes.</summary>
        public static readonly int NodeCount = Enum.GetValues(typeof(SkillNodeType)).Length;

        /// <summary>Raised after a node is bought.</summary>
        public event Action<SkillNodeType> OnPurchased;
        /// <summary>Raised after any purchase (for views refreshing everything).</summary>
        public event Action OnChanged;

        private readonly SkillNodeDef[] _defs;
        private readonly int[] _levels = new int[NodeCount];
        private readonly int _maxLevel;
        private readonly double _costGrowth;

        /// <summary>
        /// Creates a tree. <paramref name="defs"/> is indexed by <see cref="SkillNodeType"/>; each node can be bought
        /// up to <paramref name="maxLevel"/> times, level n costing round(baseCost * costGrowth^n) (n = current level).
        /// </summary>
        public SkillTree(SkillNodeDef[] defs, int maxLevel = 1, double costGrowth = 1.0)
        {
            if (defs == null || defs.Length != NodeCount)
                throw new ArgumentException("One definition per skill node required.", nameof(defs));
            _defs = (SkillNodeDef[])defs.Clone();
            _maxLevel = Math.Max(1, maxLevel);
            _costGrowth = double.IsNaN(costGrowth) ? 1.0 : Math.Max(1.0, costGrowth);
        }

        /// <summary>Highest level a node can reach.</summary>
        public int MaxLevel => _maxLevel;

        /// <summary>Current level of the node (0 = not bought).</summary>
        public int Level(SkillNodeType type) => _levels[(int)type];

        /// <summary>True once the node has at least one level.</summary>
        public bool IsOwned(SkillNodeType type) => _levels[(int)type] > 0;

        /// <summary>True when the node is at its maximum level.</summary>
        public bool IsMaxed(SkillNodeType type) => _levels[(int)type] >= _maxLevel;

        /// <summary>Currency cost of the node's next level.</summary>
        public double Cost(SkillNodeType type) =>
            Math.Round(_defs[(int)type].Cost * Math.Pow(_costGrowth, _levels[(int)type]));

        /// <summary>Effect value of ONE level of the node (see <see cref="SkillNodeDef.Value"/>).</summary>
        public float Value(SkillNodeType type) => _defs[(int)type].Value;

        /// <summary>Total effect of the node at its current level (per-level value x level).</summary>
        public float TotalValue(SkillNodeType type) => _defs[(int)type].Value * _levels[(int)type];

        /// <summary>True if the node is below its max level and the wallet covers its next-level cost.</summary>
        public bool CanBuy(SkillNodeType type, MetaWallet wallet) =>
            wallet != null && !IsMaxed(type) && wallet.CanAfford(Cost(type));

        /// <summary>Deducts currency and raises the node one level. Returns false (no change) if maxed or unaffordable.</summary>
        public bool TryBuy(SkillNodeType type, MetaWallet wallet)
        {
            if (!CanBuy(type, wallet)) return false;
            if (!wallet.TrySpend(Cost(type))) return false;
            _levels[(int)type]++;
            OnPurchased?.Invoke(type);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Story 007 restore: sets every node's level at once (indexed by <see cref="SkillNodeType"/>). Rejects the
        /// whole array (returns false, nothing changed) if its length is not <see cref="NodeCount"/> or any level is
        /// outside 0..<see cref="MaxLevel"/>. Raises <see cref="OnChanged"/> (not <see cref="OnPurchased"/>) on success.
        /// </summary>
        public bool SetLevels(int[] levels)
        {
            if (levels == null || levels.Length != NodeCount) return false;
            for (int i = 0; i < levels.Length; i++)
                if (levels[i] < 0 || levels[i] > _maxLevel) return false;
            Array.Copy(levels, _levels, NodeCount);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Copies every node's current level (indexed by <see cref="SkillNodeType"/>).</summary>
        public int[] GetLevels() => (int[])_levels.Clone();

        /// <inheritdoc />
        public float DamageMultiplier => 1f + TotalValue(SkillNodeType.ArcanePower);
        /// <inheritdoc />
        public float AttackSpeedMultiplier => 1f + TotalValue(SkillNodeType.SwiftHands);
        /// <inheritdoc />
        public float CooldownMultiplier => Math.Max(0f, 1f - TotalValue(SkillNodeType.QuickRecovery));
        /// <inheritdoc />
        public float GoldMultiplier => 1f + TotalValue(SkillNodeType.GoldenTouch);

        /// <summary>
        /// Flat tower max HP bonus: <see cref="SkillNodeType.TowerBastion"/> per-level value x its level (0 at level 0). Not part of
        /// <see cref="IStatModifierSource"/> (flat, not multiplicative); applied by a direct
        /// <see cref="Tower.AddMaxHp"/> call when a run's tower is created, the same way the shop's Tower HP
        /// upgrade is applied.
        /// </summary>
        public float TowerHpBonus => TotalValue(SkillNodeType.TowerBastion);
    }
}
