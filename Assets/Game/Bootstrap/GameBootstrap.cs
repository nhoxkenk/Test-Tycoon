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
        [SerializeField] private UpgradeConfig[] upgrades;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject plotUpgradePrefab;
        [SerializeField] private GameObject upgradeSectionPrefab;
        [SerializeField] private GameObject upgradeItemPrefab;
        [SerializeField] private GameObject payEffectPrefab;
        [SerializeField] private GameObject buildDoneEffectPrefab;

        private PlotConstructionController plotController;
        private HarvestMarketCoordinator tradeCoordinator;
        private FarmTickRunner runner;
        private ProgressionService progression;
        private UpgradeMenuView upgradeMenu;
        private ProgressPersistenceController persistence;
        private ProgressFeedbackView feedback;
        private IProgressStore progressStore;
        private ProgressSnapshot loadedSnapshot;
        private bool persistenceCanSave;

        public ProgressionService Progression => progression;

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
            if (actors == null) actors = FindObjectOfType<ActorSceneInstaller>();
            if (plots == null || plots.Length == 0) plots = FindObjectsOfType<PlotSlotView>();
            if (hud == null) hud = FindObjectOfType<WalletHudView>();
            if (mainCanvas == null)
                foreach (var canvas in FindObjectsOfType<Canvas>())
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) { mainCanvas = canvas; break; }
            if (!Money.TryParse(CurrencyId.Coin, initialBalance, out var initialCoin))
                throw new InvalidOperationException("Initial balance must be a non-negative integer.");
            var gameCamera = Camera.main;
            if (plots == null || plots.Length == 0 || actors == null || gameCamera == null)
                throw new InvalidOperationException("Farm scene references are missing.");

            var buildDefinitions = new List<PlotDefinition>(plots.Length);
            var harvestDefinitions = new List<HarvestDefinition>(plots.Length);
            var levelCosts = new Dictionary<int, Money[]>();
            var plotBuildTimes = new Dictionary<int, float>();
            foreach (var plot in plots)
            {
                if (plot == null || plot.Resource == null || plotBuildTimes.ContainsKey(plot.PlotId) ||
                    string.IsNullOrWhiteSpace(plot.Resource.ResourceId) ||
                    !Money.TryParse(CurrencyId.Coin, plot.Resource.UnlockCost, out var cost) ||
                    !Money.TryParse(CurrencyId.Coin, plot.Resource.BatchSaleValue, out var saleValue))
                    throw new InvalidOperationException("A plot has missing or invalid resource data.");

                plotBuildTimes.Add(plot.PlotId, plot.Resource.BuildSeconds);
                buildDefinitions.Add(new PlotDefinition(plot.PlotId, cost, plot.Resource.BuildSeconds));
                harvestDefinitions.Add(new HarvestDefinition(plot.PlotId, plot.Resource.ResourceId,
                    saleValue, plot.Resource.HarvestSeconds, plot.Resource.RegrowSeconds, plot.Resource.ProfitPercentPerLevel));
                var costs = new Money[9];
                var authoredCosts = plot.Resource.LevelUpgradeCosts;
                for (var i = 0; i < costs.Length; i++)
                {
                    var text = authoredCosts != null && authoredCosts.Length == 9 && !string.IsNullOrWhiteSpace(authoredCosts[i])
                        ? authoredCosts[i] : plot.Resource.UpgradeCost;
                    if (!Money.TryParse(CurrencyId.Coin, text, out costs[i]))
                        throw new InvalidOperationException("A level upgrade cost is invalid for plot " + plot.PlotId);
                }
                levelCosts.Add(plot.PlotId, costs);
            }

            var configuredUpgrades = upgrades != null && upgrades.Length > 0 ? upgrades : Resources.LoadAll<UpgradeConfig>("Upgrades");
            Array.Sort(configuredUpgrades, (left, right) => UpgradeOrder(left).CompareTo(UpgradeOrder(right)));
            upgrades = configuredUpgrades;
            var upgradeDefinitions = new List<UpgradeDefinition>();
            foreach (var config in configuredUpgrades)
            {
                if (config == null || !Money.TryParse(CurrencyId.Coin, config.CoinCost, out var upgradeCost))
                    throw new InvalidOperationException("An upgrade config is missing or has invalid cost.");
                var kind = config.UpgradeEffect == UpgradeConfig.Effect.AddCustomers ? UpgradeKind.CustomerCount :
                    config.UpgradeEffect == UpgradeConfig.Effect.DoubleAllPlots ? UpgradeKind.AllPlotsIncome : UpgradeKind.PlotIncome;
                upgradeDefinitions.Add(new UpgradeDefinition(config.Id, kind, config.Amount, config.PlotId, upgradeCost));
            }

            progressStore = new JsonProgressStore();
            var hasLoadedProgress = progressStore.TryLoad(out loadedSnapshot, out var loadError);
            persistenceCanSave = string.IsNullOrEmpty(loadError);
            if (!persistenceCanSave) Debug.LogError("Farm progress was not loaded: " + loadError + " Saving is disabled to preserve the file.", this);
            var walletBalances = new[] { initialCoin };
            if (hasLoadedProgress)
            {
                if (!ProgressSnapshotFactory.TryValidate(loadedSnapshot, plotBuildTimes, upgradeDefinitions, out var savedBalances, out var validationError))
                {
                    Debug.LogError("Farm progress was not loaded: " + validationError + " Saving is disabled to preserve the file.", this);
                    persistenceCanSave = false;
                    loadedSnapshot = null;
                }
                else walletBalances = savedBalances;
            }
            var wallet = new Wallet(walletBalances);
            var construction = new ConstructionService((IWalletTransactions)wallet, buildDefinitions);
            var harvest = new HarvestService(harvestDefinitions);
            if (loadedSnapshot != null)
            {
                if (!ProgressSnapshotFactory.RestorePlotProgress(loadedSnapshot, construction, harvest))
                    throw new InvalidOperationException("Validated plot progress could not be restored.");
            }
            var market = new MarketSaleService(harvest, (IWalletTransactions)wallet);
            if (hud != null) hud.Bind(wallet);

            plotController = new PlotConstructionController(plots, construction, wallet, gameCamera);
            plotController.Initialize();
            progression = new ProgressionService((IWalletTransactions)wallet, construction, harvest, levelCosts,
                upgradeDefinitions, actors.SetTargetCustomerCount, actors.InitialCustomerCount);
            if (loadedSnapshot != null && !progression.RestorePurchasedUpgradeIds(loadedSnapshot.purchasedUpgradeIds))
                throw new InvalidOperationException("Validated upgrade records could not be restored.");
            if (!actors.Initialize(progression.CustomerTargetCount)) throw new InvalidOperationException("Actor scene could not be initialized.");
            plotController.BindProgression(progression, mainCanvas, plotUpgradePrefab);
            if (mainCanvas != null && upgrades.Length > 0 && upgradeSectionPrefab != null && upgradeItemPrefab != null)
            {
                upgradeMenu = gameObject.AddComponent<UpgradeMenuView>();
                upgradeMenu.Initialize(mainCanvas, upgradeSectionPrefab, upgradeItemPrefab, upgrades, progression, wallet);
            }
            tradeCoordinator = new HarvestMarketCoordinator(plotController, actors, harvest, market, plots);
            feedback = gameObject.AddComponent<ProgressFeedbackView>();
            feedback.Initialize(construction, market, actors, plots, payEffectPrefab, buildDoneEffectPrefab);
            plotController.RestorePresentation();

            persistence = gameObject.AddComponent<ProgressPersistenceController>();
            persistence.Initialize(progressStore, wallet, construction, harvest, progression, persistenceCanSave);

            runner = GetComponent<FarmTickRunner>();
            if (runner == null) runner = gameObject.AddComponent<FarmTickRunner>();
            runner.Initialize(construction, harvest, plotController);
        }

        private void OnDestroy() => TearDown();

        private static int UpgradeOrder(UpgradeConfig config)
        {
            if (config == null) return int.MaxValue;
            switch (config.Id)
            {
                case "customer_plus_1": return 0;
                case "customer_plus_2": return 1;
                case "income_all_x2": return 2;
                case "income_plot_1_x2": return 3;
                case "income_plot_2_x2": return 4;
                case "income_plot_3_x2": return 5;
                case "income_plot_4_x2": return 6;
                default: return int.MaxValue;
            }
        }

        private void TearDown()
        {
            if (runner != null) runner.Stop();
            if (persistence != null) { persistence.Flush(); Destroy(persistence); }
            if (feedback != null) { feedback.Dispose(); Destroy(feedback); }
            tradeCoordinator?.Dispose();
            if (upgradeMenu != null) Destroy(upgradeMenu);
            plotController?.Dispose();
            if (actors != null) actors.Dispose();
            if (hud != null) hud.Unbind();
            tradeCoordinator = null;
            plotController = null;
            progression = null;
            upgradeMenu = null;
            persistence = null;
            feedback = null;
        }
    }
}
