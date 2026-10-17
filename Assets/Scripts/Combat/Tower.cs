using System;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>Player tower: holds HP and reports changes via events (no UI references).</summary>
    public sealed class Tower : MonoBehaviour
    {
        /// <summary>Raised when HP changes: (current, max).</summary>
        public event Action<float, float> OnHpChanged;
        /// <summary>Raised once when HP reaches 0.</summary>
        public event Action OnDestroyed;

        /// <summary>Current HP.</summary>
        public float Hp { get; private set; }
        /// <summary>Maximum HP.</summary>
        public float MaxHp { get; private set; }
        /// <summary>World X of the tower's right edge; enemies reaching it deal contact damage.</summary>
        public float ContactX { get; private set; }
        /// <summary>True while HP is above 0.</summary>
        public bool IsAlive => Hp > 0f;

        /// <summary>Initialises HP from config.</summary>
        public void Initialize(CombatConfig config, float contactX)
        {
            MaxHp = config.TowerMaxHp;
            Hp = MaxHp;
            ContactX = contactX;
            OnHpChanged?.Invoke(Hp, MaxHp);
        }

        /// <summary>Raises max HP by <paramref name="amount"/> and heals by the same amount. No effect on a destroyed tower.</summary>
        public void AddMaxHp(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            MaxHp += amount;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            OnHpChanged?.Invoke(Hp, MaxHp);
        }

        /// <summary>Updates the contact edge after a layout change.</summary>
        public void SetContactX(float contactX) => ContactX = contactX;

        /// <summary>Applies damage, clamped at 0.</summary>
        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Hp = Mathf.Max(0f, Hp - amount);
            OnHpChanged?.Invoke(Hp, MaxHp);
            if (Hp <= 0f) OnDestroyed?.Invoke();
        }
    }
}
