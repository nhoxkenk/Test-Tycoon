using System;
using Farm.Economy;
using Farm.Simulation;
using UnityEngine;

namespace Farm.UnityAdapters
{
    // Presenter for per-plot upgrade popup. Plain C# with Dispose.
    public sealed class PlotUpgradeView : IDisposable
    {
        private readonly ConstructionUpgradeView view;
        private readonly ProgressionService progression;
        private readonly IWalletReader wallet;
        private readonly int plotId;
        private bool disposed;

        public PlotUpgradeView(ConstructionUpgradeView view, int plotId, Sprite icon,
            ProgressionService progression, IWalletReader wallet)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.plotId = plotId;
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            view.SetIcon(icon);
            view.BuyClicked += OnBuy;
            view.CloseClicked += OnClose;
            progression.Changed += Refresh;
            wallet.BalanceChanged += OnBalanceChanged;
            view.gameObject.SetActive(false);
            Refresh();
        }

        public bool IsVisible => view != null && view.gameObject.activeSelf;

        public void Show()
        {
            if (view != null)
            {
                view.gameObject.SetActive(true);
                Refresh();
            }
        }

        public void Close()
        {
            if (view != null) view.gameObject.SetActive(false);
        }

        private void OnBuy()
        {
            progression.TryUpgradePlot(plotId);
            Refresh();
        }

        private void Refresh()
        {
            if (view == null || progression == null) return;
            var level = progression.GetLevel(plotId);
            var cost = progression.GetNextLevelCost(plotId).ToString();
            var cashout = progression.GetCurrentBatchValue(plotId).ToString();
            var canBuy = progression.CanUpgradePlot(plotId);
            view.Render(level, cost, cashout, canBuy);
        }

        private void OnBalanceChanged(Money _) => Refresh();
        private void OnClose() => Close();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (view != null)
            {
                view.BuyClicked -= OnBuy;
                view.CloseClicked -= OnClose;
            }
            if (progression != null) progression.Changed -= Refresh;
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
        }
    }
}
