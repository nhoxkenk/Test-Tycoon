using System;
using System.Collections.Generic;
using Farm.Economy;

namespace Farm.Simulation
{
    public static class ProgressSnapshotCodec
    {
        public static bool TryReadBalances(ProgressSnapshot snapshot, out Money[] balances, out string error)
        {
            balances = null;
            error = null;
            if (snapshot == null || snapshot.schemaVersion != ProgressSnapshot.CurrentSchemaVersion)
                return Fail("Unsupported or missing save schema.", out error);
            if (snapshot.balances == null || snapshot.balances.Length == 0)
                return Fail("Save has no currency balances.", out error);
            var parsed = new List<Money>(snapshot.balances.Length);
            var seen = new HashSet<CurrencyId>();
            foreach (var record in snapshot.balances)
            {
                if (record == null || !CurrencyIdCodec.TryFromStorageId(record.currencyId, out var currency) ||
                    !Money.TryParse(currency, record.amount, out var amount) || !seen.Add(currency))
                    return Fail("Save contains an invalid or duplicate currency balance.", out error);
                parsed.Add(amount);
            }
            if (!seen.Contains(CurrencyId.Coin))
                return Fail("Save is missing the Coin balance.", out error);
            balances = parsed.ToArray();
            return true;
        }

        private static bool Fail(string message, out string error) { error = message; return false; }
    }
}
