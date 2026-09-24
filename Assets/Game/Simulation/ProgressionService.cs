using System;
using System.Collections.Generic;
using Farm.Economy;
using Farm.Farming;

namespace Farm.Simulation
{
    public enum UpgradeKind { CustomerCount, AllPlotsIncome, PlotIncome }

    public sealed class UpgradeDefinition
    {
        public string Id { get; }
        public UpgradeKind Kind { get; }
        public int Amount { get; }
        public int PlotId { get; }
        public Money Cost { get; }

        public UpgradeDefinition(string id, UpgradeKind kind, int amount, int plotId, Money cost)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Upgrade ID is required.", nameof(id));
            Id = id; Kind = kind; Amount = amount; PlotId = plotId; Cost = cost;
        }
    }

    public sealed class ProgressionService
    {
        public const int MaxLevel = 10;
        private readonly IWalletTransactions wallet;
        private readonly ConstructionService construction;
        private readonly HarvestService harvest;
        private readonly Action<int> setCustomerTarget;
        private int customerTargetCount = 1;
        private readonly Dictionary<int, Money[]> levelCosts;
        private readonly Dictionary<string, UpgradeDefinition> upgrades = new Dictionary<string, UpgradeDefinition>();
        private readonly HashSet<string> purchased = new HashSet<string>();

        public event Action Changed;

        public ProgressionService(IWalletTransactions wallet, ConstructionService construction, HarvestService harvest,
            IDictionary<int, Money[]> levelCosts, IEnumerable<UpgradeDefinition> definitions, Action<int> setCustomerTarget,
            int initialCustomerCount = 1)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.construction = construction ?? throw new ArgumentNullException(nameof(construction));
            this.harvest = harvest ?? throw new ArgumentNullException(nameof(harvest));
            this.levelCosts = new Dictionary<int, Money[]>(levelCosts ?? throw new ArgumentNullException(nameof(levelCosts)));
            this.setCustomerTarget = setCustomerTarget ?? throw new ArgumentNullException(nameof(setCustomerTarget));
            if (initialCustomerCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCustomerCount));
            customerTargetCount = initialCustomerCount;
            foreach (var item in definitions ?? throw new ArgumentNullException(nameof(definitions))) upgrades.Add(item.Id, item);
        }

        public int GetLevel(int plotId) => harvest.GetLevel(plotId);
        public Money GetCurrentBatchValue(int plotId) => harvest.GetCurrentBatchValue(plotId);
        public bool IsPurchased(string id) => purchased.Contains(id);
        public int CustomerTargetCount => customerTargetCount;
        public string[] GetPurchasedUpgradeIds()
        {
            var ids = new string[purchased.Count];
            purchased.CopyTo(ids);
            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        public bool RestorePurchasedUpgradeIds(IEnumerable<string> ids)
        {
            if (ids == null) return false;
            var restored = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
                if (string.IsNullOrWhiteSpace(id) || !upgrades.ContainsKey(id) || purchased.Contains(id) || !restored.Add(id)) return false;
            foreach (var id in restored)
            {
                var item = upgrades[id];
                if (item.Kind == UpgradeKind.PlotIncome && !harvest.IsPlotIncomeDoubled(item.PlotId))
                {
                    var plot = construction.GetPlot(item.PlotId);
                    if (plot == null || plot.State != PlotBuildState.Ready) return false;
                }
            }
            foreach (var id in restored)
            {
                var item = upgrades[id];
                if (item.Kind == UpgradeKind.CustomerCount) customerTargetCount += item.Amount;
                var applied = item.Kind switch
                {
                    UpgradeKind.CustomerCount => item.Amount > 0,
                    UpgradeKind.AllPlotsIncome => harvest.DoubleAllIncome() || harvest.IsAllIncomeDoubled,
                    UpgradeKind.PlotIncome => harvest.DoublePlotIncome(item.PlotId) || harvest.IsPlotIncomeDoubled(item.PlotId),
                    _ => false
                };
                if (!applied) return false;
                purchased.Add(id);
            }
            Changed?.Invoke();
            return true;
        }
        public bool IsPlotReady(int plotId) => construction.GetPlot(plotId)?.State == PlotBuildState.Ready;
        public UpgradeDefinition GetUpgrade(string id) => upgrades.TryGetValue(id, out var value) ? value : null;
        public bool CanUpgradePlot(int plotId)
        {
            var state = construction.GetPlot(plotId);
            var cost = GetNextLevelCost(plotId);
            return state != null && state.State == PlotBuildState.Ready && GetLevel(plotId) < MaxLevel &&
                levelCosts.TryGetValue(plotId, out var costs) && costs.Length >= GetLevel(plotId) &&
                wallet.GetBalance(cost.Currency).Amount >= cost.Amount;
        }
        public bool CanPurchase(string id)
        {
            if (!upgrades.TryGetValue(id, out var item) || purchased.Contains(id)) return false;
            if (item.Kind == UpgradeKind.CustomerCount && item.Amount <= 0) return false;
            if (item.Kind == UpgradeKind.AllPlotsIncome && harvest.IsAllIncomeDoubled) return false;
            if (item.Kind == UpgradeKind.PlotIncome)
            {
                var plot = construction.GetPlot(item.PlotId);
                if (plot == null || plot.State != PlotBuildState.Ready || harvest.IsPlotIncomeDoubled(item.PlotId)) return false;
            }
            return wallet.GetBalance(item.Cost.Currency).Amount >= item.Cost.Amount;
        }
        public Money GetNextLevelCost(int plotId)
        {
            var level = GetLevel(plotId);
            return level > 0 && level < MaxLevel && levelCosts.TryGetValue(plotId, out var costs) && costs.Length >= level
                ? costs[level - 1] : default;
        }

        public bool TryUpgradePlot(int plotId)
        {
            var state = construction.GetPlot(plotId);
            var level = GetLevel(plotId);
            var cost = GetNextLevelCost(plotId);
            if (state == null || state.State != PlotBuildState.Ready || level >= MaxLevel ||
                !levelCosts.TryGetValue(plotId, out var costs) || costs.Length < level || !wallet.TrySpend(cost)) return false;
            if (!harvest.SetLevel(plotId, level + 1)) throw new InvalidOperationException("Plot level could not be applied after payment.");
            Changed?.Invoke();
            return true;
        }

        public bool TryPurchase(string id)
        {
            if (!CanPurchase(id) || !upgrades.TryGetValue(id, out var item)) return false;
            if (!wallet.TrySpend(item.Cost)) return false;
            var applied = item.Kind switch
            {
                UpgradeKind.CustomerCount => ApplyCustomer(item.Amount),
                UpgradeKind.AllPlotsIncome => harvest.DoubleAllIncome(),
                UpgradeKind.PlotIncome => harvest.DoublePlotIncome(item.PlotId),
                _ => false
            };
            if (!applied) throw new InvalidOperationException("Validated upgrade effect could not be applied.");
            purchased.Add(id);
            Changed?.Invoke();
            return true;
        }

        private bool ApplyCustomer(int amount)
        {
            if (amount <= 0) return false;
            customerTargetCount += amount;
            setCustomerTarget(customerTargetCount);
            return true;
        }
    }
}
