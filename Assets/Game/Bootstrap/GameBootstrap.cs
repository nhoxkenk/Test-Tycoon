using System;
using System.Collections.Generic;
using Farm.Economy;
using Farm.Farming;
using Farm.Simulation;
using Farm.UnityAdapters;
using UnityEngine;

namespace Farm.Bootstrap
{
    // Composition root: owns the scene graph, not gameplay decisions.
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string initialBalance = "100000";
        [SerializeField] private ActorSceneInstaller actors;
        [SerializeField] private PlotSlotView[] plots;
        [SerializeField] private WalletHudView hud;

        private PlotConstructionController plotController;
        private HarvestMarketCoordinator tradeCoordinator;
        private FarmTickRunner runner;

        private void Awake()
        {
            try { Compose(); }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                TearDown();
                enabled = false;
            }
        }

        private void Compose()
        {
            if (!Money.TryParse(CurrencyId.Coin, initialBalance, out var initialCoin))
                throw new InvalidOperationException("Initial balance must be a non-negative integer.");
            var gameCamera = Camera.main;
            if (plots == null || plots.Length == 0 || actors == null || gameCamera == null)
                throw new InvalidOperationException("Farm scene references are missing.");

            var buildDefinitions = new List<PlotDefinition>(plots.Length);
            var harvestDefinitions = new List<HarvestDefinition>(plots.Length);
            var plotIds = new HashSet<int>();
            foreach (var plot in plots)
            {
                if (plot == null || plot.Resource == null || !plotIds.Add(plot.PlotId) ||
                    string.IsNullOrWhiteSpace(plot.Resource.ResourceId) ||
                    !Money.TryParse(CurrencyId.Coin, plot.Resource.UnlockCost, out var cost) ||
                    !Money.TryParse(CurrencyId.Coin, plot.Resource.BatchSaleValue, out var saleValue))
                    throw new InvalidOperationException("A plot has missing or invalid resource data.");

                buildDefinitions.Add(new PlotDefinition(plot.PlotId, cost, plot.Resource.BuildSeconds));
                harvestDefinitions.Add(new HarvestDefinition(plot.PlotId, plot.Resource.ResourceId,
                    saleValue, plot.Resource.HarvestSeconds, plot.Resource.RegrowSeconds));
            }

            var wallet = new Wallet(initialCoin);
            var construction = new ConstructionService((IWalletTransactions)wallet, buildDefinitions);
            var harvest = new HarvestService(harvestDefinitions);
            var market = new MarketSaleService(harvest, (IWalletTransactions)wallet);
            if (hud != null) hud.Bind(wallet);
            if (!actors.Initialize()) throw new InvalidOperationException("Actor scene could not be initialized.");

            plotController = new PlotConstructionController(plots, construction, wallet, gameCamera);
            plotController.Initialize();
            tradeCoordinator = new HarvestMarketCoordinator(plotController, actors, harvest, market, plots);

            runner = GetComponent<FarmTickRunner>();
            if (runner == null) runner = gameObject.AddComponent<FarmTickRunner>();
            runner.Initialize(construction, harvest, plotController);
        }

        private void OnDestroy() => TearDown();

        private void TearDown()
        {
            if (runner != null) runner.Stop();
            tradeCoordinator?.Dispose();
            plotController?.Dispose();
            if (actors != null) actors.Dispose();
            if (hud != null) hud.Unbind();
            tradeCoordinator = null;
            plotController = null;
        }
    }
}
