using System;
using System.Collections.Generic;
using Farm.Actors;
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
        [SerializeField] private FarmSceneBindings scene;

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
            // 1. Validate scene bindings
            if (scene == null)
                throw new InvalidOperationException("Farm scene bindings are not assigned.");
            if (!scene.Validate(out var bindingError))
                throw new InvalidOperationException("Farm scene bindings are invalid: " + bindingError);
            if (!Money.TryParse(CurrencyId.Coin, initialBalance, out var initialCoin))
                throw new InvalidOperationException("Initial balance must be a non-negative integer.");

            // 2. Load assets based on navigation strategy
            var backendId = scene.Actors.NavigationStrategy != null ? scene.Actors.NavigationStrategy.BackendId : "NavMesh";
            var assets = FarmAssets.Load(backendId);

            // 3. Validate prefabs
            ConstructionUpgradeView.ValidatePrefab(assets.ConstructionUpgradePrefab);

            // 4. Build definitions from plot config
            var buildDefinitions = new List<PlotDefinition>(scene.Plots.Length);
            var harvestDefinitions = new List<HarvestDefinition>(scene.Plots.Length);
            var levelCosts = new Dictionary<int, Money[]>();
            var plotBuildTimes = new Dictionary<int, float>();
            foreach (var plot in scene.Plots)
            {
                if (plot == null || plot.Resource == null || plotBuildTimes.ContainsKey(plot.PlotId) ||
                    plot.GetComponent<PlotUiView>() == null ||
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

            // 5. Upgrade definitions
            var upgradeDefinitions = BuildUpgradeDefinitions(assets.UpgradeConfigs);

            // 6. Load and validate save
            progressStore = new JsonProgressStore();
            var hasLoadedProgress = progressStore.TryLoad(out loadedSnapshot, out var loadError);
            persistenceCanSave = string.IsNullOrEmpty(loadError);
            if (!persistenceCanSave) Debug.LogError("Farm progress was not loaded: " + loadError + " Saving is disabled to preserve the file.", this);

            // 7. Create services + restore progression
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
            if (scene.Hud != null) scene.Hud.Bind(wallet);

            // 8. Prepare navigation + create coordinator (no spawn yet)
            plotController = new PlotConstructionController(scene.Plots, construction, wallet, scene.GameCamera);
            plotController.Initialize();
            progression = new ProgressionService((IWalletTransactions)wallet, construction, harvest, levelCosts,
                upgradeDefinitions, scene.Actors.SetTargetCustomerCount, scene.Actors.InitialCustomerCount);
            if (loadedSnapshot != null && !progression.RestorePurchasedUpgradeIds(loadedSnapshot.purchasedUpgradeIds))
                throw new InvalidOperationException("Validated upgrade records could not be restored.");

            // 9. Initialize actors (validates + prepares navigation, then spawns)
            if (!scene.Actors.Initialize(assets.WorkerPrefab, assets.CustomerPrefab, progression.CustomerTargetCount))
                throw new InvalidOperationException("Actor scene could not be initialized.");

            // 10. Bind controllers/UI/events
            var upgradePrefab = assets.ConstructionUpgradePrefab.GetComponent<ConstructionUpgradeView>();
            plotController.BindProgression(progression, upgradePrefab);
            upgradeMenu = gameObject.AddComponent<UpgradeMenuView>();
            upgradeMenu.Initialize(scene.MainCanvas, scene.UpgradeNavButton, assets.UpgradeSectionPrefab, assets.UpgradeItemPrefab,
                assets.UpgradeConfigs, progression, wallet);
            tradeCoordinator = new HarvestMarketCoordinator(plotController, scene.Actors, harvest, market, scene.Plots);
            feedback = gameObject.AddComponent<ProgressFeedbackView>();
            feedback.Initialize(construction, market, scene.Actors, scene.Plots, assets.PayEffectPrefab, assets.BuildDoneEffectPrefab);
            plotController.RestorePresentation();

            // 11. Persistence
            persistence = gameObject.AddComponent<ProgressPersistenceController>();
            persistence.Initialize(progressStore, wallet, construction, harvest, progression, persistenceCanSave);

            runner = GetComponent<FarmTickRunner>();
            if (runner == null) runner = gameObject.AddComponent<FarmTickRunner>();
            runner.Initialize(construction, harvest, plotController);

            // 12. Cheat: reset button (bottom-left corner)
            var cheat = gameObject.AddComponent<CheatResetView>();
            cheat.Initialize(scene.MainCanvas, persistence.StopSaving);
        }

        private static List<UpgradeDefinition> BuildUpgradeDefinitions(UpgradeConfig[] configs)
        {
            Array.Sort(configs, (left, right) => UpgradeOrder(left).CompareTo(UpgradeOrder(right)));
            var definitions = new List<UpgradeDefinition>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var config in configs)
            {
                if (config == null || string.IsNullOrWhiteSpace(config.Id) || UpgradeOrder(config) == int.MaxValue ||
                    !Money.TryParse(CurrencyId.Coin, config.CoinCost, out var upgradeCost))
                    throw new InvalidOperationException("Resources/Upgrades has a missing, unknown, or invalid upgrade config: " + (config != null ? config.Id : "null") + ".");
                if (!ids.Add(config.Id))
                    throw new InvalidOperationException("Resources/Upgrades has duplicate upgrade ID: " + config.Id + ".");
                var kind = config.UpgradeEffect == UpgradeConfig.Effect.AddCustomers ? UpgradeKind.CustomerCount :
                    config.UpgradeEffect == UpgradeConfig.Effect.DoubleAllPlots ? UpgradeKind.AllPlotsIncome : UpgradeKind.PlotIncome;
                definitions.Add(new UpgradeDefinition(config.Id, kind, config.Amount, config.PlotId, upgradeCost));
            }
            return definitions;
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
            if (scene != null && scene.Actors != null) scene.Actors.Dispose();
            if (scene != null && scene.Hud != null) scene.Hud.Unbind();
            tradeCoordinator = null;
            plotController = null;
            progression = null;
            upgradeMenu = null;
            persistence = null;
            feedback = null;
        }
    }
}
