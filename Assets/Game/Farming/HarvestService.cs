using System;
using System.Collections.Generic;
using System.Numerics;
using Farm.Economy;

namespace Farm.Farming
{
    public sealed class HarvestDefinition
    {
        public int PlotId { get; }
        public string ResourceId { get; }
        public Money BatchValue { get; }
        public int ProfitPercentPerLevel { get; }
        public float HarvestSeconds { get; }
        public float RegrowSeconds { get; }

        public HarvestDefinition(int plotId, string resourceId, Money batchValue, float harvestSeconds, float regrowSeconds, int profitPercentPerLevel = 0)
        {
            if (plotId < 0) throw new ArgumentOutOfRangeException(nameof(plotId));
            if (string.IsNullOrWhiteSpace(resourceId)) throw new ArgumentException("Resource ID is required.", nameof(resourceId));
            if (harvestSeconds < 0 || float.IsNaN(harvestSeconds) || float.IsInfinity(harvestSeconds))
                throw new ArgumentOutOfRangeException(nameof(harvestSeconds));
            if (regrowSeconds < 0 || float.IsNaN(regrowSeconds) || float.IsInfinity(regrowSeconds))
                throw new ArgumentOutOfRangeException(nameof(regrowSeconds));
            if (profitPercentPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(profitPercentPerLevel));
            PlotId = plotId;
            ResourceId = resourceId;
            BatchValue = batchValue;
            ProfitPercentPerLevel = profitPercentPerLevel;
            HarvestSeconds = harvestSeconds;
            RegrowSeconds = regrowSeconds;
        }
    }

    public sealed class HarvestBatch
    {
        public long Id { get; }
        public int PlotId { get; }
        public string ResourceId { get; }
        public int Quantity { get; }
        public Money SaleValue { get; }

        internal HarvestBatch(long id, HarvestDefinition definition, Money saleValue)
        {
            Id = id;
            PlotId = definition.PlotId;
            ResourceId = definition.ResourceId;
            Quantity = 3;
            SaleValue = saleValue;
        }
    }

    public sealed class HarvestService
    {
        private sealed class PlotStock
        {
            public readonly HarvestDefinition Definition;
            public bool Active;
            public int Quantity;
            public int PendingWorker = -1;
            public bool Harvesting;
            public float HarvestRemaining;
            public bool Regrowing;
            public float RegrowRemaining;

            public PlotStock(HarvestDefinition definition) => Definition = definition;
        }

        private readonly Dictionary<int, PlotStock> plots = new Dictionary<int, PlotStock>();
        private readonly Dictionary<int, int> levels = new Dictionary<int, int>();
        private readonly HashSet<int> doubledPlots = new HashSet<int>();
        private bool allPlotsDoubled;
        private readonly Dictionary<int, HarvestBatch> batchesByWorker = new Dictionary<int, HarvestBatch>();
        private long nextBatchId = 1;

        public event Action<int, int> StockChanged;
        public event Action<int, HarvestBatch> BatchReady;

        public HarvestService(IEnumerable<HarvestDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            foreach (var definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Harvest definition is null.", nameof(definitions));
                plots.Add(definition.PlotId, new PlotStock(definition));
                levels.Add(definition.PlotId, 1);
            }
        }

        public int GetLevel(int plotId) => levels.TryGetValue(plotId, out var level) ? level : 0;
        public Money GetCurrentBatchValue(int plotId) => plots.TryGetValue(plotId, out var plot) ? CalculateValue(plot) : default;
        public bool SetLevel(int plotId, int level)
        {
            if (!levels.ContainsKey(plotId) || level < 1 || level > 10) return false;
            levels[plotId] = level;
            return true;
        }
        public bool IsPlotIncomeDoubled(int plotId) => doubledPlots.Contains(plotId);
        public bool IsAllIncomeDoubled => allPlotsDoubled;
        public bool DoublePlotIncome(int plotId) => plots.ContainsKey(plotId) && doubledPlots.Add(plotId);
        public bool DoubleAllIncome()
        {
            if (allPlotsDoubled) return false;
            allPlotsDoubled = true;
            return true;
        }

        public bool ActivatePlot(int plotId)
        {
            if (!plots.TryGetValue(plotId, out var plot) || plot.Active) return false;
            plot.Active = true;
            plot.Quantity = 3;
            StockChanged?.Invoke(plotId, plot.Quantity);
            return true;
        }

        public int GetStock(int plotId) => plots.TryGetValue(plotId, out var plot) ? plot.Quantity : 0;
        public bool TryGetBatch(int workerId, out HarvestBatch batch) => batchesByWorker.TryGetValue(workerId, out batch);

        public bool RequestHarvest(int workerId, int plotId)
        {
            if (!plots.TryGetValue(plotId, out var plot) || !plot.Active || batchesByWorker.ContainsKey(workerId))
                return false;
            if (plot.PendingWorker == workerId) return true;
            if (plot.PendingWorker >= 0) return false;
            plot.PendingWorker = workerId;
            StartIfReady(plot);
            return true;
        }

        public void CancelWorker(int workerId)
        {
            foreach (var plot in plots.Values)
            {
                if (plot.PendingWorker != workerId) continue;
                plot.PendingWorker = -1;
                plot.Harvesting = false;
                plot.HarvestRemaining = 0;
            }
        }

        public bool RemoveBatch(int workerId, long batchId)
        {
            if (!batchesByWorker.TryGetValue(workerId, out var batch) || batch.Id != batchId) return false;
            batchesByWorker.Remove(workerId);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            foreach (var plot in plots.Values)
            {
                if (!plot.Active) continue;
                var remaining = deltaTime;
                for (var step = 0; step < 3; step++)
                {
                    if (plot.Regrowing)
                    {
                        if (plot.RegrowRemaining > remaining)
                        {
                            plot.RegrowRemaining -= remaining;
                            break;
                        }
                        remaining -= plot.RegrowRemaining;
                        plot.Regrowing = false;
                        plot.Quantity = 3;
                        StockChanged?.Invoke(plot.Definition.PlotId, plot.Quantity);
                        StartIfReady(plot);
                        continue;
                    }
                    if (!plot.Harvesting) break;
                    if (plot.HarvestRemaining > remaining)
                    {
                        plot.HarvestRemaining -= remaining;
                        break;
                    }
                    remaining -= plot.HarvestRemaining;
                    plot.Harvesting = false;
                    var workerId = plot.PendingWorker;
                    plot.PendingWorker = -1;
                    plot.Quantity = 0;
                    plot.Regrowing = true;
                    plot.RegrowRemaining = plot.Definition.RegrowSeconds;
                    var saleValue = CalculateValue(plot);
                    var batch = new HarvestBatch(nextBatchId++, plot.Definition, saleValue);
                    batchesByWorker.Add(workerId, batch);
                    StockChanged?.Invoke(plot.Definition.PlotId, 0);
                    BatchReady?.Invoke(workerId, batch);
                }
            }
        }

        private Money CalculateValue(PlotStock plot)
        {
            var plotId = plot.Definition.PlotId;
            var ratios = new List<MoneyRatio>(3)
            {
                new MoneyRatio(new BigInteger(100) + new BigInteger((levels[plotId] - 1)) * plot.Definition.ProfitPercentPerLevel, 100)
            };
            if (doubledPlots.Contains(plotId)) ratios.Add(new MoneyRatio(2, 1));
            if (allPlotsDoubled) ratios.Add(new MoneyRatio(2, 1));
            return MoneyMath.FloorAfterRatios(plot.Definition.BatchValue, ratios);
        }

        private static void StartIfReady(PlotStock plot)
        {
            if (plot.PendingWorker < 0 || plot.Quantity != 3 || plot.Harvesting) return;
            plot.Harvesting = true;
            plot.HarvestRemaining = plot.Definition.HarvestSeconds;
        }
    }
}
