namespace ArcaneSpire.Combat
{
    /// <summary>
    /// The five permanent skill-tree nodes (Story 006, design/game-brief.md MVP feature 6). Each is leveled (up to a
    /// configured max, cost growing per level) and bought with <see cref="MetaWallet"/> currency. Enum order = panel order and hotkey order
    /// (Q, W, E, R, F). Four reuse <see cref="CombatStats.IStatModifierSource"/>'s multiplier categories directly
    /// (so <see cref="SkillTree"/> needs no new math in <see cref="CombatStats"/>, exactly how runes already work);
    /// <see cref="TowerBastion"/> is a flat tower-HP bonus applied the same way the shop's Tower HP upgrade is
    /// (a direct <see cref="Tower.AddMaxHp"/> call), because flat HP has no multiplier slot to plug into.
    /// </summary>
    public enum SkillNodeType
    {
        /// <summary>Permanent +damage % (feeds <see cref="CombatStats.IStatModifierSource.DamageMultiplier"/>).</summary>
        ArcanePower = 0,
        /// <summary>Permanent +attack speed % (feeds <see cref="CombatStats.IStatModifierSource.AttackSpeedMultiplier"/>).</summary>
        SwiftHands = 1,
        /// <summary>Permanent -cooldown % (feeds <see cref="CombatStats.IStatModifierSource.CooldownMultiplier"/>).</summary>
        QuickRecovery = 2,
        /// <summary>Permanent +gold % (feeds <see cref="CombatStats.IStatModifierSource.GoldMultiplier"/>).</summary>
        GoldenTouch = 3,
        /// <summary>Permanent flat tower max HP, applied directly at tower creation.</summary>
        TowerBastion = 4
    }

    /// <summary>Static definition of one skill node, read from config: its currency cost and effect value.</summary>
    public readonly struct SkillNodeDef
    {
        /// <summary>Run-currency cost of the first level.</summary>
        public readonly double Cost;
        /// <summary>Effect value PER LEVEL: a fractional bonus/reduction for the four multiplier nodes, or flat HP for Tower Bastion.</summary>
        public readonly float Value;

        /// <summary>Creates a definition.</summary>
        public SkillNodeDef(double cost, float value)
        {
            Cost = cost;
            Value = value;
        }
    }
}
