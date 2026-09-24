using System;

namespace Farm.Economy
{
    // Save IDs are explicit so enum order and numeric values never become the save format.
    public static class CurrencyIdCodec
    {
        public static string ToStorageId(CurrencyId currency)
        {
            switch (currency)
            {
                case CurrencyId.Coin: return "coin";
                case CurrencyId.Gem: return "gem";
                default: throw new ArgumentOutOfRangeException(nameof(currency));
            }
        }

        public static bool TryFromStorageId(string id, out CurrencyId currency)
        {
            switch (id)
            {
                case "coin": currency = CurrencyId.Coin; return true;
                case "gem": currency = CurrencyId.Gem; return true;
                default: currency = default; return false;
            }
        }
    }
}
