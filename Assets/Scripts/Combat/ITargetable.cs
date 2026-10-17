using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Something the mage can target.</summary>
    public interface ITargetable
    {
        /// <summary>True while the target can still be attacked.</summary>
        bool IsAlive { get; }
        /// <summary>World position used for range and distance checks.</summary>
        Vector2 Position { get; }
    }
}
