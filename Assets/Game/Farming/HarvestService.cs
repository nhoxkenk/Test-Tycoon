using System;
using System.Collections.Generic;
using Farm.Economy;

namespace Farm.Farming
{
    public sealed class HarvestDefinition
    {
        public int PlotId { get; }
        public string ResourceId { get; }
        public Money BatchValue { get; }
        public float HarvestSeconds { get; }
        public float RegrowSeconds { get; }

        public HarvestDefinition(int plotId, string resourceId, Money batchValue, float harvestSeconds, float regrowSeconds)
        {
            if (plotId < 0) throw new ArgumentOutOfRangeException(nameof(plotId));
            if (string.IsNullOrWhiteSpace(resourceId)) throw new ArgumentException("Resource ID is required.", nameof(resourceId));
            if (harvestSeconds < 0 || float.IsNaN(harvestSeconds) || float.IsInfinity(harvestSeconds))
                throw new ArgumentOutOfRangeException(nameof(harvestSeconds));
            if (regrowSeconds < 0 || float.IsNaN(regrowSeconds) || float.IsInfinity(regrowSeconds))
                throw new ArgumentOutOfRangeException(nameof(regrowSeconds));
            PlotId = plotId;
            ResourceId = resourceId;
            BatchValue = batchValue;
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
            }
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
                    var saleValue = MoneyMath.FloorAfterRatios(plot.Definition.BatchValue, Array.Empty<MoneyRatio>());
                    var batch = new HarvestBatch(nextBatchId++, plot.Definition, saleValue);
                    batchesByWorker.Add(workerId, batch);
                    StockChanged?.Invoke(plot.Definition.PlotId, 0);
                    BatchReady?.Invoke(workerId, batch);
                }
            }
        }

        private static void StartIfReady(PlotStock plot)
        {
            if (plot.PendingWorker < 0 || plot.Quantity != 3 || plot.Harvesting) return;
            plot.Harvesting = true;
            plot.HarvestRemaining = plot.Definition.HarvestSeconds;
        }
    }
}
