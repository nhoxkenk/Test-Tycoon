using System.Globalization;
using System.Numerics;

namespace Farm.Economy
{
    public static class MoneyDisplayFormatter
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T" };

        public static string FormatHud(Money money)
        {
            var amount = money.Amount;
            if (amount < 1000) return amount.ToString("N0", CultureInfo.InvariantCulture);

            var digits = amount.ToString(CultureInfo.InvariantCulture);
            var group = (digits.Length - 1) / 3;
            if (group >= Suffixes.Length)
                return digits[0] + (digits.Length > 1 ? "." + digits[1] : string.Empty) + "e" + (digits.Length - 1);

            var divisor = BigInteger.Pow(1000, group);
            var whole = BigInteger.DivRem(amount, divisor, out var remainder);
            var tenths = (int)(remainder * 10 / divisor);
            var wholeText = whole.ToString(CultureInfo.InvariantCulture);
            return tenths == 0 ? wholeText + Suffixes[group] : wholeText + "." + tenths + Suffixes[group];
        }
    }
}
