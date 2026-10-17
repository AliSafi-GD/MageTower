namespace ArcaneSpire.Combat
{
    /// <summary>The four shop upgrades. Enum order = button order = keyboard shortcut order (1-4).</summary>
    public enum UpgradeType
    {
        /// <summary>Mage damage per attack.</summary>
        Damage = 0,
        /// <summary>Mage attacks per second.</summary>
        AttackSpeed = 1,
        /// <summary>Tower maximum HP.</summary>
        TowerHp = 2,
        /// <summary>Chance for an attack to crit.</summary>
        CritChance = 3
    }
}
