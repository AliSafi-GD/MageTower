using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Plain C# spell state (no UnityEngine dependency): unlocked flag and cooldown timer per <see cref="SpellType"/>.
    /// Cooldown semantics match <see cref="AttackCooldown"/>: the timer keeps running (clamped at 0) while no target
    /// exists, so a ready spell fires at once when a target appears; frame overshoot is carried into the next interval.
    /// Story 004 (design/game-brief.md MVP feature 4). Spells start locked.
    /// </summary>
    public sealed class SpellBook
    {
        /// <summary>Number of spell types.</summary>
        public static readonly int SpellCount = Enum.GetValues(typeof(SpellType)).Length;

        /// <summary>Raised once when a spell becomes unlocked.</summary>
        public event Action<SpellType> OnUnlocked;

        private readonly Func<SpellType, float> _cooldownFunc;
        private readonly bool[] _unlocked = new bool[SpellCount];
        private readonly float[] _remaining = new float[SpellCount];

        /// <summary>Creates a book; <paramref name="cooldownFunc"/> maps a spell to its cooldown in seconds (evaluated on use).</summary>
        public SpellBook(Func<SpellType, float> cooldownFunc)
        {
            _cooldownFunc = cooldownFunc ?? throw new ArgumentNullException(nameof(cooldownFunc));
        }

        /// <summary>True once the spell has been unlocked.</summary>
        public bool IsUnlocked(SpellType type) => _unlocked[(int)type];

        /// <summary>Unlocks the spell (ready immediately). Returns false if it was already unlocked.</summary>
        public bool Unlock(SpellType type)
        {
            if (_unlocked[(int)type]) return false;
            _unlocked[(int)type] = true;
            _remaining[(int)type] = 0f;
            OnUnlocked?.Invoke(type);
            return true;
        }

        /// <summary>Full cooldown of the spell in seconds.</summary>
        public float Cooldown(SpellType type) => Math.Max(0f, SafeCooldown(type));

        /// <summary>
        /// Caps every running timer at its current full cooldown. Call after the cooldown function's result shrinks
        /// (e.g. a cooldown rune was equipped) so the new value applies to the in-flight cooldown immediately.
        /// </summary>
        public void ClampRemainingToCooldown()
        {
            for (int i = 0; i < SpellCount; i++)
            {
                float cd = Cooldown((SpellType)i);
                if (_remaining[i] > cd) _remaining[i] = cd;
            }
        }

        /// <summary>Seconds until the spell is ready (0 = ready).</summary>
        public float Remaining(SpellType type) => _remaining[(int)type];

        /// <summary>
        /// Advances the spell's timer; returns true if it should be cast this tick (unlocked, ready, target present).
        /// Locked spells never tick or fire.
        /// </summary>
        public bool Tick(SpellType type, float deltaTime, bool hasTarget)
        {
            int i = (int)type;
            if (!_unlocked[i]) return false;
            _remaining[i] -= deltaTime;
            if (!hasTarget)
            {
                if (_remaining[i] < 0f) _remaining[i] = 0f;
                return false;
            }
            if (_remaining[i] > 0f) return false;
            _remaining[i] = Math.Max(0f, _remaining[i] + Cooldown(type));
            return true;
        }

        private float SafeCooldown(SpellType type)
        {
            float c = _cooldownFunc(type);
            return float.IsNaN(c) || float.IsInfinity(c) ? 0f : c;
        }
    }
}
