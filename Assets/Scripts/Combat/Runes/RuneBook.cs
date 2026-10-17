using System;
using System.Collections.Generic;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Plain C# rune state (no UnityEngine dependency): unlocked flags, equipped set (limited slots) and the
    /// aggregated multipliers. Story 005 (design/game-brief.md MVP feature 5). Runes are unlocked with gold only
    /// (run currency does not exist yet). Multipliers are recomputed from scratch from the equipped set on every
    /// change, in fixed rune order, so unequipping restores the previous values exactly. Nothing here is touched
    /// by wave logic: equipped runes persist across waves.
    /// </summary>
    public sealed class RuneBook
    {
        /// <summary>Number of rune types.</summary>
        public static readonly int RuneCount = Enum.GetValues(typeof(RuneType)).Length;

        /// <summary>Raised once when a rune becomes unlocked.</summary>
        public event Action<RuneType> OnUnlocked;
        /// <summary>Raised when a rune is equipped.</summary>
        public event Action<RuneType> OnEquipped;
        /// <summary>Raised when a rune is unequipped.</summary>
        public event Action<RuneType> OnUnequipped;
        /// <summary>Raised after any unlock/equip/unequip, after the multipliers are updated.</summary>
        public event Action OnChanged;

        private readonly GoldWallet _wallet;
        private readonly RuneDef[] _defs;
        private readonly float _minCooldownMultiplier;
        private readonly bool[] _unlocked = new bool[RuneCount];
        private readonly bool[] _equipped = new bool[RuneCount];
        private readonly List<RuneModifier> _scratch = new List<RuneModifier>(RuneCount);

        /// <summary>Creates a book. <paramref name="defs"/> is indexed by <see cref="RuneType"/>.</summary>
        public RuneBook(GoldWallet wallet, RuneDef[] defs, int slotCount, float minCooldownMultiplier)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            if (defs == null || defs.Length != RuneCount) throw new ArgumentException("One definition per rune required.", nameof(defs));
            _defs = (RuneDef[])defs.Clone();
            SlotCount = Math.Max(0, slotCount);
            _minCooldownMultiplier = minCooldownMultiplier;
            Recompute();
        }

        /// <summary>Wallet runes are bought from.</summary>
        public GoldWallet Wallet => _wallet;
        /// <summary>Maximum number of equipped runes.</summary>
        public int SlotCount { get; }
        /// <summary>Number of currently equipped runes.</summary>
        public int EquippedCount { get; private set; }
        /// <summary>Number of unlocked runes.</summary>
        public int UnlockedCount { get; private set; }
        /// <summary>Damage factor from equipped runes (1 = none); applies to basic attack and spells.</summary>
        public float DamageMultiplier { get; private set; } = 1f;
        /// <summary>Kill gold factor from equipped runes (1 = none).</summary>
        public float GoldMultiplier { get; private set; } = 1f;
        /// <summary>Cooldown factor from equipped runes (1 = none, floored above 0).</summary>
        public float CooldownMultiplier { get; private set; } = 1f;

        /// <summary>True once the rune is unlocked.</summary>
        public bool IsUnlocked(RuneType type) => _unlocked[(int)type];
        /// <summary>True while the rune is equipped.</summary>
        public bool IsEquipped(RuneType type) => _equipped[(int)type];
        /// <summary>Gold cost to unlock.</summary>
        public double UnlockCost(RuneType type) => _defs[(int)type].UnlockCost;
        /// <summary>Modifier type of the rune.</summary>
        public RuneModifierType ModifierType(RuneType type) => _defs[(int)type].ModifierType;
        /// <summary>Fractional modifier value of the rune.</summary>
        public float Value(RuneType type) => _defs[(int)type].Value;

        /// <summary>True if the rune is locked and the wallet covers its cost.</summary>
        public bool CanUnlock(RuneType type) => !_unlocked[(int)type] && _wallet.CanAfford(_defs[(int)type].UnlockCost);

        /// <summary>True if the rune is unlocked, not equipped and a slot is free.</summary>
        public bool CanEquip(RuneType type) => _unlocked[(int)type] && !_equipped[(int)type] && EquippedCount < SlotCount;

        /// <summary>Spends gold and unlocks the rune (not equipped). Returns false if already unlocked or unaffordable.</summary>
        public bool TryUnlock(RuneType type)
        {
            int i = (int)type;
            if (_unlocked[i] || !_wallet.TrySpend(_defs[i].UnlockCost)) return false;
            _unlocked[i] = true;
            UnlockedCount++;
            OnUnlocked?.Invoke(type);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Equips an unlocked rune if a slot is free. Returns false otherwise.</summary>
        public bool TryEquip(RuneType type)
        {
            if (!CanEquip(type)) return false;
            _equipped[(int)type] = true;
            EquippedCount++;
            Recompute();
            OnEquipped?.Invoke(type);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Unequips the rune; the stats are recomputed from the remaining set. Returns false if it was not equipped.</summary>
        public bool Unequip(RuneType type)
        {
            if (!_equipped[(int)type]) return false;
            _equipped[(int)type] = false;
            EquippedCount--;
            Recompute();
            OnUnequipped?.Invoke(type);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Panel/hotkey action: locked -> unlock (if affordable) and equip if a slot is free; unlocked -> equip;
        /// equipped -> unequip. Returns true if anything changed.
        /// </summary>
        public bool Activate(RuneType type)
        {
            if (_equipped[(int)type]) return Unequip(type);
            if (!_unlocked[(int)type])
            {
                if (!TryUnlock(type)) return false;
                TryEquip(type);
                return true;
            }
            return TryEquip(type);
        }

        private void Recompute()
        {
            _scratch.Clear();
            for (int i = 0; i < RuneCount; i++) // fixed order: result independent of equip order
                if (_equipped[i]) _scratch.Add(new RuneModifier(_defs[i].ModifierType, _defs[i].Value));
            RuneEffects.Combine(_scratch, _minCooldownMultiplier, out float d, out float g, out float c);
            DamageMultiplier = d;
            GoldMultiplier = g;
            CooldownMultiplier = c;
        }
    }
}
