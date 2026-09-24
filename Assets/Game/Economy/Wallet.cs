using System.Collections.Generic;
using System.Numerics;

namespace Farm.Economy
{
    public sealed class Wallet
    {
        private readonly Dictionary<CurrencyId, BigInteger> balances = new Dictionary<CurrencyId, BigInteger>();

        public Wallet(Money initialBalance)
        {
            balances[initialBalance.Currency] = initialBalance.Amount;
        }

        public Money GetBalance(CurrencyId currency)
        {
            return new Money(currency, balances.TryGetValue(currency, out var amount) ? amount : BigInteger.Zero);
        }

        public bool TrySpend(Money cost)
        {
            var balance = GetBalance(cost.Currency).Amount;
            if (balance < cost.Amount)
                return false;

            balances[cost.Currency] = balance - cost.Amount;
            return true;
        }

        public void Credit(Money amount)
        {
            balances[amount.Currency] = GetBalance(amount.Currency).Amount + amount.Amount;
        }
    }
}
