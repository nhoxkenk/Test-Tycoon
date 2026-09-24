using System;
using System.Collections.Generic;
using Farm.Economy;
using Farm.Farming;

namespace Farm.Simulation
{
    public static class ProgressSnapshotFactory
    {
        public static ProgressSnapshot Capture(Wallet wallet, ConstructionService construction,
            HarvestService harvest, ProgressionService progression)
        {
            var result = new ProgressSnapshot();
            var balances = wallet.GetBalances();
            result.balances = new BalanceRecord[balances.Length];
            for (var i = 0; i < balances.Length; i++)
                result.balances[i] = new BalanceRecord
                {
                    currencyId = CurrencyIdCodec.ToStorageId(balances[i].Currency),
                    amount = balances[i].Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                };
            var sessions = new List<PlotSession>();
            foreach (var id in construction.GetPlotIds()) sessions.Add(construction.GetPlot(id));
            sessions.Sort((a, b) => a.Definition.Id.CompareTo(b.Definition.Id));
            result.plots = new PlotRecord[sessions.Count];
            for (var i = 0; i < sessions.Count; i++)
            {
                var plot = sessions[i];
                result.plots[i] = new PlotRecord
                {
                    plotId = plot.Definition.Id,
                    state = StateToId(plot.State),
                    remainingBuildSeconds = plot.State == PlotBuildState.Building ? plot.RemainingBuildSeconds : 0,
                    level = harvest.GetLevel(plot.Definition.Id)
                };
            }
            result.purchasedUpgradeIds = progression.GetPurchasedUpgradeIds();
            return result;
        }

        public static bool TryValidate(ProgressSnapshot snapshot, IDictionary<int, float> plotBuildTimes,
            IEnumerable<UpgradeDefinition> upgradeDefinitions, out Money[] balances, out string error)
        {
            if (!ProgressSnapshotCodec.TryReadBalances(snapshot, out balances, out error)) return false;
            if (snapshot.plots == null || snapshot.purchasedUpgradeIds == null)
                return Fail("Save is missing progression records.", out balances, out error);
            var expected = new HashSet<int>(plotBuildTimes.Keys);
            if (snapshot.plots.Length != expected.Count)
                return Fail("Save plot records do not match this farm.", out balances, out error);
            var seen = new HashSet<int>();
            foreach (var plot in snapshot.plots)
            {
                if (plot == null || !expected.Contains(plot.plotId) || !seen.Add(plot.plotId) || plot.level < 1 || plot.level > 10 ||
                    !TryState(plot.state, out var state) || float.IsNaN(plot.remainingBuildSeconds) || float.IsInfinity(plot.remainingBuildSeconds) ||
                    plot.remainingBuildSeconds < 0 || (state != PlotBuildState.Building && plot.remainingBuildSeconds != 0) ||
                    plot.remainingBuildSeconds > plotBuildTimes[plot.plotId] || (state != PlotBuildState.Ready && plot.level != 1))
                    return Fail("Save contains an invalid plot record.", out balances, out error);
            }
            var known = new Dictionary<string, UpgradeDefinition>(StringComparer.Ordinal);
            foreach (var definition in upgradeDefinitions) known.Add(definition.Id, definition);
            var purchases = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in snapshot.purchasedUpgradeIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !known.TryGetValue(id, out var definition) || !purchases.Add(id) ||
                    (definition.Kind == UpgradeKind.CustomerCount && definition.Amount <= 0) ||
                    (definition.Kind == UpgradeKind.PlotIncome &&
                     !Array.Exists(snapshot.plots, plot => plot.plotId == definition.PlotId && plot.state == "ready")))
                    return Fail("Save contains an unknown or duplicate upgrade record.", out balances, out error);
            }
            error = null;
            return true;
        }

        public static bool RestorePlotProgress(ProgressSnapshot snapshot, ConstructionService construction, HarvestService harvest)
        {
            foreach (var record in snapshot.plots)
            {
                if (!TryState(record.state, out var state) || !construction.RestorePlot(record.plotId, state, record.remainingBuildSeconds) ||
                    !harvest.SetLevel(record.plotId, record.level)) return false;
            }
            return true;
        }

        public static bool TryState(string id, out PlotBuildState state)
        {
            switch (id)
            {
                case "locked": state = PlotBuildState.Locked; return true;
                case "building": state = PlotBuildState.Building; return true;
                case "ready": state = PlotBuildState.Ready; return true;
                default: state = default; return false;
            }
        }

        private static string StateToId(PlotBuildState state) => state switch
        {
            PlotBuildState.Locked => "locked",
            PlotBuildState.Building => "building",
            PlotBuildState.Ready => "ready",
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        private static bool Fail(string message, out Money[] balances, out string error)
        {
            balances = null;
            error = message;
            return false;
        }
    }
}
