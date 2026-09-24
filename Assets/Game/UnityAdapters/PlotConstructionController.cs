using System;
using System.Collections.Generic;
using Farm.Economy;
using Farm.Farming;
using UnityEngine;

namespace Farm.UnityAdapters
{
    // Owns the interaction and presentation lifecycle of all construction plots.
    public sealed class PlotConstructionController : IDisposable
    {
        private readonly PlotSlotView[] plots;
        private readonly ConstructionService construction;
        private readonly IWalletReader wallet;
        private readonly Camera gameCamera;
        private readonly Dictionary<int, PlotSlotView> plotById = new Dictionary<int, PlotSlotView>();
        private readonly Dictionary<int, PlotUiView> uiById = new Dictionary<int, PlotUiView>();
        private bool initialized;

        public event Action<PlotSlotView> ConstructionVisible;

        public PlotConstructionController(PlotSlotView[] plots, ConstructionService construction,
            IWalletReader wallet, Camera gameCamera)
        {
            this.plots = plots ?? throw new ArgumentNullException(nameof(plots));
            this.construction = construction ?? throw new ArgumentNullException(nameof(construction));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.gameCamera = gameCamera ?? throw new ArgumentNullException(nameof(gameCamera));
        }

        public void Initialize()
        {
            if (initialized) return;
            foreach (var plot in plots)
            {
                if (plot == null || !plotById.TryAdd(plot.PlotId, plot))
                    throw new InvalidOperationException("A plot is missing or has a duplicate ID.");
                var ui = plot.GetComponent<PlotUiView>();
                if (ui == null) throw new InvalidOperationException("Plot UI is missing on plot " + plot.PlotId);
                uiById.Add(plot.PlotId, ui);
            }

            initialized = true;
            construction.PlotChanged += OnPlotChanged;
            foreach (var plot in plots)
            {
                var ui = uiById[plot.PlotId];
                ui.Initialize(plot.PlotId, plot.Resource, gameCamera);
                plot.BuildRequested += OnBuildRequested;
                plot.ConstructionVisible += OnConstructionVisible;
                ui.UnlockConfirmed += OnUnlockConfirmed;
            }
        }

        public void TickPresentation()
        {
            foreach (var entry in uiById)
            {
                var state = construction.GetPlot(entry.Key);
                if (state.State == PlotBuildState.Building)
                    entry.Value.UpdateBuild(state.RemainingBuildSeconds);
            }
        }

        private void OnBuildRequested(int plotId)
        {
            foreach (var ui in uiById.Values) ui.HideUnlock();
            var cost = construction.GetPlot(plotId).Definition.BuildCost;
            uiById[plotId].ShowUnlock(wallet.GetBalance(cost.Currency).Amount >= cost.Amount);
        }

        private void OnUnlockConfirmed(int plotId)
        {
            if (!construction.TryStartBuild(plotId)) return;
            plotById[plotId].BeginBuild();
            uiById[plotId].ShowBuilding();
        }

        private void OnPlotChanged(PlotSession state)
        {
            if (state.State == PlotBuildState.Ready)
                plotById[state.Definition.Id].MarkReady();
        }

        private void OnConstructionVisible(PlotSlotView plot)
        {
            uiById[plot.PlotId].ShowInformation();
            ConstructionVisible?.Invoke(plot);
        }

        public void Dispose()
        {
            if (!initialized) return;
            initialized = false;
            construction.PlotChanged -= OnPlotChanged;
            foreach (var plot in plots)
            {
                if (plot == null) continue;
                plot.BuildRequested -= OnBuildRequested;
                plot.ConstructionVisible -= OnConstructionVisible;
                if (uiById.TryGetValue(plot.PlotId, out var ui))
                    ui.UnlockConfirmed -= OnUnlockConfirmed;
            }
            ConstructionVisible = null;
        }
    }
}
