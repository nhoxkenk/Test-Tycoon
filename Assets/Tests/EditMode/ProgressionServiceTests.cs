using System.Collections.Generic;
using System.Numerics;
using System.IO;
using Farm.Economy;
using Farm.Farming;
using Farm.Simulation;
using Farm.UnityAdapters;
using NUnit.Framework;
using UnityEngine;

namespace Farm.Tests
{
    public sealed class ProgressionServiceTests
    {
        [Test]
        public void HudMoneyFormattingUsesIntegerMathForLargeAmounts()
        {
            Assert.That(MoneyDisplayFormatter.FormatHud(new Money(CurrencyId.Coin, 999)), Is.EqualTo("999"));
            Assert.That(MoneyDisplayFormatter.FormatHud(new Money(CurrencyId.Coin, 1234)), Is.EqualTo("1.2K"));
            Assert.That(MoneyDisplayFormatter.FormatHud(new Money(CurrencyId.Coin, 1234567890000)), Is.EqualTo("1.2T"));
            Assert.That(MoneyDisplayFormatter.FormatHud(new Money(CurrencyId.Coin,
                BigInteger.Parse("922337203685477580812345"))), Is.EqualTo("9.2e23"));
        }

        [Test]
        public void ExistingHarvestBatchKeepsItsPriceAfterLaterModifiers()
        {
            var harvest = new HarvestService(new[] { new HarvestDefinition(1, "wheat", new Money(CurrencyId.Coin, 101), 0, 1, 10) });
            Assert.That(harvest.ActivatePlot(1), Is.True);
            Assert.That(harvest.RequestHarvest(7, 1), Is.True);
            harvest.Tick(0);
            Assert.That(harvest.TryGetBatch(7, out var existingBatch), Is.True);
            Assert.That(existingBatch.SaleValue.Amount, Is.EqualTo(new BigInteger(101)));

            harvest.SetLevel(1, 2);
            harvest.DoublePlotIncome(1);
            harvest.DoubleAllIncome();
            Assert.That(existingBatch.SaleValue.Amount, Is.EqualTo(new BigInteger(101)));
            Assert.That(harvest.GetCurrentBatchValue(1).Amount, Is.EqualTo(new BigInteger(444)));
        }

        [Test]
        public void FeedbackEventsOnlyFireAfterCommittedBuildAndSale()
        {
            var wallet = new Wallet(new Money(CurrencyId.Coin, 20));
            var construction = new ConstructionService(wallet,
                new[] { new PlotDefinition(1, new Money(CurrencyId.Coin, 5), 2) });
            var buildStarted = 0;
            var buildCompleted = 0;
            construction.BuildStarted += _ => buildStarted++;
            construction.BuildCompleted += _ => buildCompleted++;
            Assert.That(construction.TryStartBuild(1), Is.True);
            Assert.That(buildStarted, Is.EqualTo(1));
            Assert.That(buildCompleted, Is.Zero);
            construction.Tick(1.9f);
            Assert.That(buildCompleted, Is.Zero);
            construction.Tick(.2f);
            Assert.That(buildCompleted, Is.EqualTo(1));

            var harvest = new HarvestService(new[] { new HarvestDefinition(1, "wheat", new Money(CurrencyId.Coin, 9), 0, 1) });
            harvest.ActivatePlot(1);
            harvest.RequestHarvest(7, 1);
            harvest.Tick(0);
            var market = new MarketSaleService(harvest, wallet);
            var sales = 0;
            market.SaleCommitted += (_, __) => sales++;
            Assert.That(market.TrySell(7, 9, (_, __, ___) => false), Is.False);
            Assert.That(sales, Is.Zero);
            var beforeSale = wallet.GetBalance(CurrencyId.Coin).Amount;
            Assert.That(market.TrySell(7, 9, (_, __, ___) => true), Is.True);
            Assert.That(sales, Is.EqualTo(1));
            Assert.That(wallet.GetBalance(CurrencyId.Coin).Amount, Is.EqualTo(beforeSale + 9));
        }

        [Test]
        public void LevelAndIncomeModifiersCombineBeforeOneFloorAndCustomerPurchasesAdd()
        {
            var wallet = new Wallet(new Money(CurrencyId.Coin, 1000));
            var construction = new ConstructionService(wallet, new[]
            {
                new PlotDefinition(1, new Money(CurrencyId.Coin, 0), 0)
            });
            Assert.That(construction.TryStartBuild(1), Is.True);
            construction.Tick(0);
            var harvest = new HarvestService(new[]
            {
                new HarvestDefinition(1, "wheat", new Money(CurrencyId.Coin, 103), 0, 1, 10)
            });
            var costs = new Dictionary<int, Money[]> { { 1, new[]
            {
                new Money(CurrencyId.Coin, 1), new Money(CurrencyId.Coin, 2),
                new Money(CurrencyId.Coin, 3), new Money(CurrencyId.Coin, 4),
                new Money(CurrencyId.Coin, 5), new Money(CurrencyId.Coin, 6),
                new Money(CurrencyId.Coin, 7), new Money(CurrencyId.Coin, 8),
                new Money(CurrencyId.Coin, 9)
            } } };
            var definitions = new[]
            {
                new UpgradeDefinition("customer_1", UpgradeKind.CustomerCount, 1, 0, new Money(CurrencyId.Coin, 10)),
                new UpgradeDefinition("customer_2", UpgradeKind.CustomerCount, 2, 0, new Money(CurrencyId.Coin, 20)),
                new UpgradeDefinition("farm_x2", UpgradeKind.AllPlotsIncome, 1, 0, new Money(CurrencyId.Coin, 30)),
                new UpgradeDefinition("plot_x2", UpgradeKind.PlotIncome, 1, 1, new Money(CurrencyId.Coin, 40)),
                new UpgradeDefinition("locked_plot", UpgradeKind.PlotIncome, 1, 2, new Money(CurrencyId.Coin, 50)),
                new UpgradeDefinition("too_expensive", UpgradeKind.CustomerCount, 9, 0, new Money(CurrencyId.Coin, 9000))
            };
            var targetCustomerCount = 1;
            var progression = new ProgressionService(wallet, construction, harvest, costs, definitions,
                target => targetCustomerCount = target);

            Assert.That(progression.TryUpgradePlot(1), Is.True);
            Assert.That(progression.TryUpgradePlot(1), Is.True);
            Assert.That(progression.GetLevel(1), Is.EqualTo(3));
            Assert.That(progression.TryPurchase("plot_x2"), Is.True);
            Assert.That(progression.TryPurchase("farm_x2"), Is.True);
            Assert.That(progression.TryPurchase("customer_1"), Is.True);
            Assert.That(progression.TryPurchase("customer_2"), Is.True);
            Assert.That(targetCustomerCount, Is.EqualTo(4));
            Assert.That(progression.CustomerTargetCount, Is.EqualTo(4));
            var balanceAfterPurchases = wallet.GetBalance(CurrencyId.Coin).Amount;
            Assert.That(balanceAfterPurchases.ToString(), Is.EqualTo("897"));
            Assert.That(progression.TryPurchase("locked_plot"), Is.False);
            Assert.That(progression.TryPurchase("too_expensive"), Is.False);
            Assert.That(progression.TryPurchase("farm_x2"), Is.False);
            Assert.That(wallet.GetBalance(CurrencyId.Coin).Amount, Is.EqualTo(balanceAfterPurchases));

            Assert.That(harvest.ActivatePlot(1), Is.True);
            Assert.That(harvest.RequestHarvest(17, 1), Is.True);
            harvest.Tick(0);
            Assert.That(harvest.TryGetBatch(17, out var batch), Is.True);
            Assert.That(batch.SaleValue.Amount.ToString(), Is.EqualTo("494")); // floor(103 * 1.2 * 2 * 2)
            var balance = wallet.GetBalance(CurrencyId.Coin).Amount;
            Assert.That(progression.TryPurchase("customer_1"), Is.False);
            Assert.That(wallet.GetBalance(CurrencyId.Coin).Amount, Is.EqualTo(balance));
        }

        [Test]
        public void SnapshotPreservesLargeBalancesBuildTimerAndPurchasedCustomerEffectsOnce()
        {
            var large = BigInteger.Parse("922337203685477580812345");
            var wallet = new Wallet(new[] { new Money(CurrencyId.Coin, large), new Money(CurrencyId.Gem, 7) });
            var definitions = new[]
            {
                new PlotDefinition(1, new Money(CurrencyId.Coin, 0), 20),
                new PlotDefinition(2, new Money(CurrencyId.Coin, 0), 0)
            };
            var construction = new ConstructionService(wallet, definitions);
            Assert.That(construction.TryStartBuild(1), Is.True);
            construction.Tick(6);
            Assert.That(construction.TryStartBuild(2), Is.True);
            construction.Tick(0);
            var harvest = new HarvestService(new[]
            {
                new HarvestDefinition(1, "wheat", new Money(CurrencyId.Coin, 5), 1, 1),
                new HarvestDefinition(2, "wood", new Money(CurrencyId.Coin, 5), 1, 1)
            });
            harvest.SetLevel(2, 3);
            var upgrades = new[]
            {
                new UpgradeDefinition("customer_plus_1", UpgradeKind.CustomerCount, 1, 0, new Money(CurrencyId.Coin, 1)),
                new UpgradeDefinition("customer_plus_2", UpgradeKind.CustomerCount, 2, 0, new Money(CurrencyId.Coin, 1)),
                new UpgradeDefinition("farm_x2", UpgradeKind.AllPlotsIncome, 1, 0, new Money(CurrencyId.Coin, 1)),
                new UpgradeDefinition("plot2_x2", UpgradeKind.PlotIncome, 1, 2, new Money(CurrencyId.Coin, 1))
            };
            var progression = new ProgressionService(wallet, construction, harvest,
                new Dictionary<int, Money[]> { { 1, new Money[9] }, { 2, new Money[9] } }, upgrades, _ => { });
            progression.RestorePurchasedUpgradeIds(new[] { "customer_plus_1", "customer_plus_2", "farm_x2", "plot2_x2" });

            var snapshot = ProgressSnapshotFactory.Capture(wallet, construction, harvest, progression);
            snapshot = JsonUtility.FromJson<ProgressSnapshot>(JsonUtility.ToJson(snapshot));
            Assert.That(snapshot.balances[0].amount, Is.EqualTo(large.ToString()));
            Assert.That(snapshot.balances[0].currencyId, Is.EqualTo("coin"));
            Assert.That(snapshot.plots[0].state, Is.EqualTo("building"));
            Assert.That(snapshot.plots[0].remainingBuildSeconds, Is.EqualTo(14));
            Assert.That(snapshot.plots[0].level, Is.EqualTo(1));
            Assert.That(snapshot.plots[1].state, Is.EqualTo("ready"));
            Assert.That(snapshot.plots[1].level, Is.EqualTo(3));
            Assert.That(ProgressSnapshotFactory.TryValidate(snapshot, new Dictionary<int, float> { { 1, 20 }, { 2, 0 } },
                upgrades, out var restoredBalances, out var error), Is.True, error);
            Assert.That(restoredBalances[0].Amount, Is.EqualTo(large));

            var restoredWallet = new Wallet(restoredBalances);
            var restoredConstruction = new ConstructionService(restoredWallet, definitions);
            var restoredHarvest = new HarvestService(new[]
            {
                new HarvestDefinition(1, "wheat", new Money(CurrencyId.Coin, 5), 1, 1),
                new HarvestDefinition(2, "wood", new Money(CurrencyId.Coin, 5), 1, 1)
            });
            Assert.That(ProgressSnapshotFactory.RestorePlotProgress(snapshot, restoredConstruction, restoredHarvest), Is.True);
            Assert.That(restoredConstruction.GetPlot(1).State, Is.EqualTo(PlotBuildState.Building));
            Assert.That(restoredConstruction.GetPlot(2).State, Is.EqualTo(PlotBuildState.Ready));
            Assert.That(restoredHarvest.GetLevel(2), Is.EqualTo(3));
            var restoredProgression = new ProgressionService(restoredWallet, restoredConstruction, restoredHarvest,
                new Dictionary<int, Money[]> { { 1, new Money[9] }, { 2, new Money[9] } }, upgrades, _ => { });
            Assert.That(restoredProgression.RestorePurchasedUpgradeIds(snapshot.purchasedUpgradeIds), Is.True);
            Assert.That(restoredProgression.CustomerTargetCount, Is.EqualTo(4));
            Assert.That(restoredProgression.IsPurchased("customer_plus_2"), Is.True);
            Assert.That(restoredHarvest.IsAllIncomeDoubled, Is.True);
            Assert.That(restoredHarvest.IsPlotIncomeDoubled(2), Is.True);
            Assert.That(restoredProgression.RestorePurchasedUpgradeIds(snapshot.purchasedUpgradeIds), Is.False);
            Assert.That(restoredProgression.CustomerTargetCount, Is.EqualTo(4));
        }

        [Test]
        public void SnapshotRejectsUnsupportedSchemaCorruptMoneyAndDuplicatePlots()
        {
            var snapshot = new ProgressSnapshot
            {
                balances = new[] { new BalanceRecord { currencyId = "coin", amount = "not-money" } },
                plots = new[] { new PlotRecord { plotId = 1, state = "locked", level = 1 } },
                purchasedUpgradeIds = new string[0]
            };
            var times = new Dictionary<int, float> { { 1, 20 }, { 2, 20 } };
            Assert.That(ProgressSnapshotFactory.TryValidate(snapshot, times, new UpgradeDefinition[0], out _, out _), Is.False);
            snapshot.balances[0].amount = "10";
            snapshot.schemaVersion = 999;
            Assert.That(ProgressSnapshotFactory.TryValidate(snapshot, times, new UpgradeDefinition[0], out _, out _), Is.False);
            snapshot.schemaVersion = ProgressSnapshot.CurrentSchemaVersion;
            snapshot.plots = new[]
            {
                new PlotRecord { plotId = 1, state = "locked", level = 1 },
                new PlotRecord { plotId = 1, state = "locked", level = 1 }
            };
            Assert.That(ProgressSnapshotFactory.TryValidate(snapshot, times, new UpgradeDefinition[0], out _, out _), Is.False);
        }

        [Test]
        public void JsonStoreRoundTripsAndLeavesCorruptFileUntouched()
        {
            var fileName = "farm-progress-test-" + System.Guid.NewGuid().ToString("N") + ".json";
            var path = Path.Combine(Application.persistentDataPath, fileName);
            var store = new JsonProgressStore(fileName);
            try
            {
                Assert.That(store.TryLoad(out _, out var missingError), Is.False);
                Assert.That(missingError, Is.Null);
                store.Save(new ProgressSnapshot
                {
                    balances = new[] { new BalanceRecord { currencyId = "coin", amount = "922337203685477580812345" } },
                    plots = new[] { new PlotRecord { plotId = 1, state = "locked", level = 1 } },
                    purchasedUpgradeIds = new string[0]
                });
                Assert.That(store.TryLoad(out var restored, out var loadError), Is.True, loadError);
                Assert.That(restored.balances[0].amount, Is.EqualTo("922337203685477580812345"));
                store.Save(new ProgressSnapshot
                {
                    balances = new[] { new BalanceRecord { currencyId = "coin", amount = "2" } },
                    plots = new[] { new PlotRecord { plotId = 1, state = "locked", level = 1 } },
                    purchasedUpgradeIds = new string[0]
                });
                Assert.That(File.Exists(path + ".bak"), Is.True);
                Assert.That(store.TryLoad(out restored, out loadError), Is.True, loadError);
                Assert.That(restored.balances[0].amount, Is.EqualTo("2"));

                var corruptJson = "{";
                File.WriteAllText(path, corruptJson);
                Assert.That(store.TryLoad(out _, out var corruptError), Is.False);
                Assert.That(corruptError, Is.Not.Null.And.Not.Empty);
                Assert.That(File.ReadAllText(path), Is.EqualTo(corruptJson));
                File.Delete(path);
                Assert.That(store.TryLoad(out _, out var missingPrimaryError), Is.False);
                Assert.That(missingPrimaryError, Is.Not.Null.And.Not.Empty);
                Assert.That(File.Exists(path + ".bak"), Is.True);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
                if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
            }
        }
    }
}
