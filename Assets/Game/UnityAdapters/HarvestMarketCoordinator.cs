using System;
using System.Collections.Generic;
using Farm.Actors;
using Farm.Farming;
using Farm.Simulation;
using UnityEngine;

namespace Farm.UnityAdapters
{
    // Connects the pure harvest/sale use cases to pooled actors and plot visuals.
    public sealed class HarvestMarketCoordinator : IDisposable
    {
        private readonly PlotConstructionController constructionController;
        private readonly ActorSceneInstaller actors;
        private readonly HarvestService harvest;
        private readonly MarketSaleService market;
        private readonly Dictionary<int, PlotSlotView> plotById = new Dictionary<int, PlotSlotView>();

        public HarvestMarketCoordinator(PlotConstructionController constructionController,
            ActorSceneInstaller actors, HarvestService harvest, MarketSaleService market,
            IEnumerable<PlotSlotView> plots)
        {
            this.constructionController = constructionController ?? throw new ArgumentNullException(nameof(constructionController));
            this.actors = actors ?? throw new ArgumentNullException(nameof(actors));
            this.harvest = harvest ?? throw new ArgumentNullException(nameof(harvest));
            this.market = market ?? throw new ArgumentNullException(nameof(market));
            foreach (var plot in plots) plotById.Add(plot.PlotId, plot);

            constructionController.ConstructionVisible += OnConstructionVisible;
            harvest.StockChanged += OnStockChanged;
            harvest.BatchReady += OnBatchReady;
            actors.HarvestRequested += OnHarvestRequested;
            actors.SaleRequested += OnSaleRequested;
        }

        private void OnConstructionVisible(PlotSlotView plot)
        {
            harvest.ActivatePlot(plot.PlotId);
            if (!actors.RegisterResource(plot.PlotId, plot.HarvestPoint))
                Debug.LogError("Worker could not be assigned to plot " + plot.PlotId, plot);
        }

        private void OnStockChanged(int plotId, int quantity) => plotById[plotId].ShowStock(quantity);

        private void OnHarvestRequested(int workerId, int plotId)
        {
            if (actors.CanHarvest(workerId, plotId)) harvest.RequestHarvest(workerId, plotId);
        }

        private void OnBatchReady(int workerId, HarvestBatch batch)
        {
            if (actors.CompleteHarvest(workerId, batch.Quantity, plotById[batch.PlotId].StockPositions)) return;
            harvest.RemoveBatch(workerId, batch.Id);
            Debug.LogError("Harvest finished after worker became unavailable.");
        }

        private void OnSaleRequested(int workerId, int customerId)
        {
            if (!market.TrySell(workerId, customerId, actors.ConfirmSale))
                Debug.LogWarning("Sale request could not be completed.");
        }

        public void Dispose()
        {
            constructionController.ConstructionVisible -= OnConstructionVisible;
            harvest.StockChanged -= OnStockChanged;
            harvest.BatchReady -= OnBatchReady;
            actors.HarvestRequested -= OnHarvestRequested;
            actors.SaleRequested -= OnSaleRequested;
        }
    }
}
