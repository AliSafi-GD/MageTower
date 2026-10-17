namespace ArcaneSpire.Combat
{
    /// <summary>The three auto-cast spells. Enum order = shop order = shortcut order (5-7). Story 004.</summary>
    public enum SpellType
    {
        /// <summary>Area damage around the targeted enemy.</summary>
        Fireball = 0,
        /// <summary>Slows enemies around the target for a duration.</summary>
        Frost = 1,
        /// <summary>Chain damage jumping between nearby enemies.</summary>
        Lightning = 2
    }
}
