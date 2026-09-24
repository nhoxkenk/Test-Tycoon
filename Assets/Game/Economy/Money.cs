using System;
using System.Globalization;
using System.Numerics;

namespace Farm.Economy
{
    public readonly struct Money : IEquatable<Money>
    {
        public CurrencyId Currency { get; }
        public BigInteger Amount { get; }

        public Money(CurrencyId currency, BigInteger amount)
        {
            if (!Enum.IsDefined(typeof(CurrencyId), currency))
                throw new ArgumentOutOfRangeException(nameof(currency));

            if (amount < BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(amount), "Money cannot be negative.");

            Currency = currency;
            Amount = amount;
        }

        public static bool TryParse(CurrencyId currency, string text, out Money money)
        {
            if (Enum.IsDefined(typeof(CurrencyId), currency) &&
                BigInteger.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var amount) && amount >= BigInteger.Zero)
            {
                money = new Money(currency, amount);
                return true;
            }

            money = default;
            return false;
        }

        public bool Equals(Money other) => Currency == other.Currency && Amount.Equals(other.Amount);

        public override bool Equals(object obj) => obj is Money other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Currency, Amount);

        public override string ToString() => Amount.ToString(CultureInfo.InvariantCulture);
    }
}
