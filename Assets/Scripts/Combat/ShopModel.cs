using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Plain C# upgrade state (no UnityEngine dependency): a level per <see cref="UpgradeType"/> and
    /// purchasing against a <see cref="GoldWallet"/>. The cost curve is injected so all constants stay in config.
    /// Story 003 (design/game-brief.md MVP feature 3).
    /// </summary>
    public sealed class ShopModel
    {
        /// <summary>Number of upgrade types.</summary>
        public static readonly int UpgradeCount = Enum.GetValues(typeof(UpgradeType)).Length;

        /// <summary>Raised after a successful purchase: (upgrade, new level).</summary>
        public event Action<UpgradeType, int> OnPurchased;
        /// <summary>Raised after any purchase (for views refreshing everything).</summary>
        public event Action OnChanged;

        private readonly GoldWallet _wallet;
        private readonly Func<UpgradeType, int, double> _costFunc;
        private readonly Func<UpgradeType, int, bool> _isMaxedFunc;
        private readonly int[] _levels = new int[UpgradeCount];
        private readonly SpellBook _spells;
        private readonly Func<SpellType, double> _spellCostFunc;

        /// <summary>
        /// Creates a shop. <paramref name="costFunc"/> maps (upgrade, current level) to the cost of the next level;
        /// optional <paramref name="isMaxedFunc"/> maps (upgrade, current level) to whether no further level can be bought.
        /// Optional <paramref name="spells"/> + <paramref name="spellCostFunc"/> add one-time spell unlocks (Story 004).
        /// </summary>
        public ShopModel(GoldWallet wallet, Func<UpgradeType, int, double> costFunc, Func<UpgradeType, int, bool> isMaxedFunc = null,
            SpellBook spells = null, Func<SpellType, double> spellCostFunc = null)
        {
            _spells = spells;
            _spellCostFunc = spellCostFunc;
            _isMaxedFunc = isMaxedFunc;
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _costFunc = costFunc ?? throw new ArgumentNullException(nameof(costFunc));
        }

        /// <summary>The wallet purchases draw from.</summary>
        public GoldWallet Wallet => _wallet;

        /// <summary>Current level (0 = not bought).</summary>
        public int GetLevel(UpgradeType type) => _levels[(int)type];

        /// <summary>Cost of the next level of the upgrade.</summary>
        public double GetCost(UpgradeType type) => _costFunc(type, _levels[(int)type]);

        /// <summary>True when the upgrade is at its cap and cannot be bought further.</summary>
        public bool IsMaxed(UpgradeType type) => _isMaxedFunc != null && _isMaxedFunc(type, _levels[(int)type]);

        /// <summary>True if the upgrade is not maxed and the wallet covers the next level.</summary>
        public bool CanBuy(UpgradeType type) => !IsMaxed(type) && _wallet.CanAfford(GetCost(type));

        /// <summary>Deducts gold, raises the level and fires events. Returns false (no change) if maxed or unaffordable.</summary>
        public bool TryBuy(UpgradeType type)
        {
            if (IsMaxed(type)) return false;
            if (!_wallet.TrySpend(GetCost(type))) return false;
            int level = ++_levels[(int)type];
            OnPurchased?.Invoke(type, level);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>True if this shop offers spell unlocks.</summary>
        public bool HasSpells => _spells != null && _spellCostFunc != null;

        /// <summary>True once the spell has been bought.</summary>
        public bool IsSpellOwned(SpellType type) => _spells != null && _spells.IsUnlocked(type);

        /// <summary>Gold cost to unlock the spell.</summary>
        public double GetSpellCost(SpellType type) => _spellCostFunc != null ? _spellCostFunc(type) : double.PositiveInfinity;

        /// <summary>True if the spell is not owned yet and the wallet covers its cost.</summary>
        public bool CanBuySpell(SpellType type) => HasSpells && !IsSpellOwned(type) && _wallet.CanAfford(GetSpellCost(type));

        /// <summary>Deducts gold and unlocks the spell. Returns false (no change) if owned, unaffordable or unavailable.</summary>
        public bool TryBuySpell(SpellType type)
        {
            if (!HasSpells || IsSpellOwned(type)) return false;
            if (!_wallet.TrySpend(GetSpellCost(type))) return false;
            _spells.Unlock(type);
            OnChanged?.Invoke();
            return true;
        }
    }
}
