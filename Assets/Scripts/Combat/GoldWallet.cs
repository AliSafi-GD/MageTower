using System;

namespace ArcaneSpire.Combat
{
    /// <summary>Plain C# gold balance (no UnityEngine dependency). Story 003.</summary>
    public sealed class GoldWallet
    {
        /// <summary>Raised whenever the balance changes: (new amount).</summary>
        public event Action<double> OnGoldChanged;

        /// <summary>Current gold.</summary>
        public double Amount { get; private set; }

        /// <summary>Adds gold; non-positive or non-finite amounts are ignored.</summary>
        public void Add(double amount)
        {
            if (!(amount > 0.0) || double.IsInfinity(amount)) return;
            Amount += amount;
            OnGoldChanged?.Invoke(Amount);
        }

        /// <summary>True if the balance covers the cost.</summary>
        public bool CanAfford(double cost) => cost >= 0.0 && Amount >= cost;

        /// <summary>Deducts the cost if affordable; returns whether it succeeded.</summary>
        public bool TrySpend(double cost)
        {
            if (!CanAfford(cost)) return false;
            if (cost == 0.0) return true;
            Amount -= cost;
            OnGoldChanged?.Invoke(Amount);
            return true;
        }
    }
}
