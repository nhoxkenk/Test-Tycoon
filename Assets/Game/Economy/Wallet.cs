using System;
using System.Collections.Generic;
using System.Numerics;

namespace Farm.Economy
{
    public sealed class Wallet : IWalletTransactions
    {
        private readonly Dictionary<CurrencyId, BigInteger> balances = new Dictionary<CurrencyId, BigInteger>();

        public event Action<Money> BalanceChanged;

        public Wallet(Money initialBalance) : this(new[] { initialBalance }) { }

        public Wallet(IEnumerable<Money> initialBalances)
        {
            if (initialBalances == null) throw new ArgumentNullException(nameof(initialBalances));
            foreach (var balance in initialBalances)
            {
                if (balances.ContainsKey(balance.Currency))
                    throw new ArgumentException("A currency has more than one initial balance.", nameof(initialBalances));
                balances.Add(balance.Currency, balance.Amount);
            }
        }

        public Money GetBalance(CurrencyId currency)
        {
            return new Money(currency, balances.TryGetValue(currency, out var amount) ? amount : BigInteger.Zero);
        }

        // Copy for persistence; callers cannot change the wallet through this array.
        public Money[] GetBalances()
        {
            var snapshot = new Money[balances.Count];
            var index = 0;
            foreach (var balance in balances)
                snapshot[index++] = new Money(balance.Key, balance.Value);
            Array.Sort(snapshot, (left, right) => left.Currency.CompareTo(right.Currency));
            return snapshot;
        }

        bool IWalletTransactions.TrySpend(Money cost)
        {
            var balance = GetBalance(cost.Currency).Amount;
            if (balance < cost.Amount)
                return false;

            if (cost.Amount.IsZero) return true;
            balances[cost.Currency] = balance - cost.Amount;
            BalanceChanged?.Invoke(new Money(cost.Currency, balances[cost.Currency]));
            return true;
        }

        void IWalletTransactions.Credit(Money amount)
        {
            if (amount.Amount.IsZero) return;
            balances[amount.Currency] = GetBalance(amount.Currency).Amount + amount.Amount;
            BalanceChanged?.Invoke(new Money(amount.Currency, balances[amount.Currency]));
        }
    }
}
