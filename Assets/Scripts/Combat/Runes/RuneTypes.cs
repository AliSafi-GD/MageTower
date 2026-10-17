namespace ArcaneSpire.Combat
{
    /// <summary>The three runes (Story 005, design/game-brief.md MVP feature 5). Order = panel order and hotkey order (8, 9, 0).</summary>
    public enum RuneType
    {
        /// <summary>Default modifier: +damage %.</summary>
        Might = 0,
        /// <summary>Default modifier: +gold %.</summary>
        Greed = 1,
        /// <summary>Default modifier: -cooldown % (spells and basic attack interval).</summary>
        Haste = 2
    }

    /// <summary>
    /// Modifier type of a rune. Values of the same type add together; different types multiply
    /// (see <see cref="RuneEffects.Combine"/>). Serialized by value in CombatConfig, so do not reorder.
    /// </summary>
    public enum RuneModifierType
    {
        /// <summary>Fractional bonus to all damage (basic attack and spells).</summary>
        Damage = 0,
        /// <summary>Fractional bonus to kill gold.</summary>
        Gold = 1,
        /// <summary>Fractional reduction of spell cooldowns and the basic attack interval.</summary>
        Cooldown = 2
    }

    /// <summary>One active modifier: its type and fractional value (0.25 = 25%).</summary>
    public readonly struct RuneModifier
    {
        /// <summary>Which modifier type this is.</summary>
        public readonly RuneModifierType Type;
        /// <summary>Fractional value (bonus for Damage/Gold, reduction for Cooldown).</summary>
        public readonly float Value;

        /// <summary>Creates a modifier.</summary>
        public RuneModifier(RuneModifierType type, float value)
        {
            Type = type;
            Value = value;
        }
    }

    /// <summary>Static definition of one rune, read from config.</summary>
    public readonly struct RuneDef
    {
        /// <summary>Gold cost to unlock.</summary>
        public readonly double UnlockCost;
        /// <summary>Modifier type this rune grants.</summary>
        public readonly RuneModifierType ModifierType;
        /// <summary>Fractional modifier value.</summary>
        public readonly float Value;

        /// <summary>Creates a definition.</summary>
        public RuneDef(double unlockCost, RuneModifierType modifierType, float value)
        {
            UnlockCost = unlockCost;
            ModifierType = modifierType;
            Value = value;
        }
    }
}
