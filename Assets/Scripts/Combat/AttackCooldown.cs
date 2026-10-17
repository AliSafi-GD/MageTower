using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Pure attack-rate timer. The cooldown keeps running while idle, so an attack fires
    /// immediately when a target appears after the mage has been idle. While a target is
    /// held, frame overshoot is carried into the next interval so the real rate matches config.
    /// The rate is clamped to a maximum; non-finite rates are treated as that maximum.
    /// </summary>
    public sealed class AttackCooldown
    {
        /// <summary>Default rate ceiling if none is supplied.</summary>
        public const float DefaultMaxAttacksPerSecond = 20f;

        private readonly float _maxAttacksPerSecond;
        private float _interval;
        private float _remaining;

        /// <summary>Creates a cooldown for the given attacks per second (0 or less = never attacks), clamped to <paramref name="maxAttacksPerSecond"/>.</summary>
        public AttackCooldown(float attacksPerSecond, float maxAttacksPerSecond = DefaultMaxAttacksPerSecond)
        {
            _maxAttacksPerSecond = float.IsNaN(maxAttacksPerSecond) || float.IsInfinity(maxAttacksPerSecond) || maxAttacksPerSecond <= 0f
                ? DefaultMaxAttacksPerSecond
                : maxAttacksPerSecond;
            SetAttacksPerSecond(attacksPerSecond);
        }

        /// <summary>Changes the attack rate on the fly; the time already elapsed in the current interval is kept.</summary>
        public void SetAttacksPerSecond(float attacksPerSecond)
        {
            float rate = float.IsNaN(attacksPerSecond) || float.IsInfinity(attacksPerSecond)
                ? _maxAttacksPerSecond
                : Math.Min(attacksPerSecond, _maxAttacksPerSecond);
            _interval = rate > 0f ? 1f / rate : 0f;
        }

        /// <summary>Advances time; returns true if an attack should fire this tick.</summary>
        public bool Tick(float deltaTime, bool hasTarget)
        {
            if (_interval <= 0f) return false;
            _remaining -= deltaTime;
            if (!hasTarget)
            {
                // Idle: no carried debt, so the next target is attacked immediately.
                if (_remaining < 0f) _remaining = 0f;
                return false;
            }
            if (_remaining > 0f) return false;
            // Carry the overshoot (_remaining <= 0) into the next interval; clamp for huge frame spikes.
            _remaining = Math.Max(0f, _remaining + _interval);
            return true;
        }
    }
}
