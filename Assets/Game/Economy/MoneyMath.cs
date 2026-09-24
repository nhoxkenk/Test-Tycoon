using System;
using System.Collections.Generic;
using System.Numerics;

namespace Farm.Economy
{
    public readonly struct MoneyRatio
    {
        public BigInteger Numerator { get; }
        public BigInteger Denominator { get; }

        public MoneyRatio(BigInteger numerator, BigInteger denominator)
        {
            if (numerator < BigInteger.Zero) throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= BigInteger.Zero) throw new ArgumentOutOfRangeException(nameof(denominator));
            Numerator = numerator;
            Denominator = denominator;
        }
    }

    public static class MoneyMath
    {
        // Combine every modifier exactly, then discard the fractional part once.
        // Farming calls this when it freezes a batch's SaleValue at harvest completion.
        public static Money FloorAfterRatios(Money baseValue, IEnumerable<MoneyRatio> ratios)
        {
            if (ratios == null) throw new ArgumentNullException(nameof(ratios));
            var numerator = baseValue.Amount;
            var denominator = BigInteger.One;
            foreach (var ratio in ratios)
            {
                if (ratio.Denominator <= BigInteger.Zero || ratio.Numerator < BigInteger.Zero)
                    throw new ArgumentException("A money ratio is invalid.", nameof(ratios));
                numerator *= ratio.Numerator;
                denominator *= ratio.Denominator;
            }
            return new Money(baseValue.Currency, numerator / denominator);
        }
    }
}
