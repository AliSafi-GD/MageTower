using System;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Plain C# permanent run-currency balance (no UnityEngine dependency). Story 006
    /// (design/game-brief.md MVP feature 6). Separate from <see cref="GoldWallet"/>: gold is run-local and resets
    /// every run, this persists across runs (in-memory only this story; Story 007 saves it). Mirrors
    /// <see cref="GoldWallet"/>'s shape exactly so a future save/load pass treats both the same way.
    /// </summary>
    public sealed class MetaWallet
    {
        /// <summary>Raised whenever the balance changes: (new amount).</summary>
        public event Action<double> OnCurrencyChanged;

        /// <summary>Current run currency.</summary>
        public double Amount { get; private set; }

        /// <summary>Adds currency; non-positive or non-finite amounts are ignored. The balance saturates at double.MaxValue.</summary>
        public void Add(double amount)
        {
            if (!(amount > 0.0) || double.IsInfinity(amount)) return;
            Amount = Math.Min(double.MaxValue, Amount + amount); // overflow gives +Infinity; never store it
            OnCurrencyChanged?.Invoke(Amount);
        }

        /// <summary>
        /// Story 007 restore: replaces the balance with a loaded value. Negative or non-finite values are rejected
        /// (returns false, balance unchanged). Raises <see cref="OnCurrencyChanged"/> so views refresh.
        /// </summary>
        public bool Set(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0.0) return false;
            Amount = amount;
            OnCurrencyChanged?.Invoke(Amount);
            return true;
        }

        /// <summary>True if the balance covers the cost.</summary>
        public bool CanAfford(double cost) => cost >= 0.0 && Amount >= cost;

        /// <summary>Deducts the cost if affordable; returns whether it succeeded.</summary>
        public bool TrySpend(double cost)
        {
            if (!CanAfford(cost)) return false;
            if (cost == 0.0) return true;
            Amount -= cost;
            OnCurrencyChanged?.Invoke(Amount);
            return true;
        }
    }
}
