using System;

namespace Farm.Simulation
{
    [Serializable]
    public sealed class ProgressSnapshot
    {
        public const int CurrentSchemaVersion = 1;
        public int schemaVersion = CurrentSchemaVersion;
        public BalanceRecord[] balances = Array.Empty<BalanceRecord>();
        public PlotRecord[] plots = Array.Empty<PlotRecord>();
        public string[] purchasedUpgradeIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class BalanceRecord { public string currencyId; public string amount; }

    [Serializable]
    public sealed class PlotRecord
    {
        public int plotId;
        public string state;
        public float remainingBuildSeconds;
        public int level = 1;
    }

    public interface IProgressStore
    {
        bool TryLoad(out ProgressSnapshot snapshot, out string error);
        void Save(ProgressSnapshot snapshot);
    }
}
