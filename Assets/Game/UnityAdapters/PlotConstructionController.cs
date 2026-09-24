using System;
using System.Collections.Generic;
using Farm.Economy;
using Farm.Farming;
using Farm.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private bool initialized;
        private ProgressionService progression;
        private int openUpgradePlotId = -1;

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
                plot.UpgradeRequested += OnUpgradeRequested;
                ui.UnlockConfirmed += OnUnlockConfirmed;
            }
        }

        public void TickPresentation()
        {
            if (Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began) RouteScreenClick(touch.position, touch.fingerId);
                }
            }
            else if (Input.GetMouseButtonDown(0)) RouteScreenClick(Input.mousePosition, -1);

            foreach (var entry in uiById)
            {
                var state = construction.GetPlot(entry.Key);
                if (state.State == PlotBuildState.Building)
                    entry.Value.UpdateBuild(state.RemainingBuildSeconds);
            }
        }

        private void RouteScreenClick(Vector2 position, int pointerId)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                uiHits.Clear();
                eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = position, pointerId = pointerId }, uiHits);
                foreach (var uiHit in uiHits)
                    if (uiHit.module is GraphicRaycaster) return;
            }

            if (Physics.Raycast(gameCamera.ScreenPointToRay(position), out var hit, 1000f))
            {
                var plot = hit.collider.GetComponentInParent<PlotSlotView>();
                if (plot != null && plotById.ContainsKey(plot.PlotId))
                {
                    plot.RequestInteraction();
                    return;
                }
            }

            CloseOpenUpgrade();
            foreach (var ui in uiById.Values) ui.HideInteractiveUi();
        }

        public void RestorePresentation()
        {
            foreach (var plot in plots)
            {
                var state = construction.GetPlot(plot.PlotId);
                if (state.State == PlotBuildState.Building)
                {
                    plot.BeginBuild();
                    uiById[plot.PlotId].ShowBuilding();
                }
                else if (state.State == PlotBuildState.Ready) plot.MarkReady();
            }
        }

        public void BindProgression(ProgressionService service, ConstructionUpgradeView upgradePrefab)
        {
            progression = service ?? throw new ArgumentNullException(nameof(service));
            foreach (var plot in plots)
                uiById[plot.PlotId].BindProgression(plot.PlotId, progression, wallet, upgradePrefab);
        }

        private void OnBuildRequested(int plotId)
        {
            CloseOpenUpgrade();
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

        private void OnUpgradeRequested(PlotSlotView plot)
        {
            CloseOpenUpgrade();
            openUpgradePlotId = plot.PlotId;
            uiById[plot.PlotId].ShowUpgrade();
        }

        private void CloseOpenUpgrade()
        {
            if (openUpgradePlotId >= 0 && uiById.TryGetValue(openUpgradePlotId, out var openUi))
                openUi.CloseUpgrade();
            openUpgradePlotId = -1;
        }

        public void Dispose()
        {
            if (!initialized) return;
            initialized = false;
            CloseOpenUpgrade();
            construction.PlotChanged -= OnPlotChanged;
            foreach (var plot in plots)
            {
                if (plot == null) continue;
                plot.BuildRequested -= OnBuildRequested;
                plot.ConstructionVisible -= OnConstructionVisible;
                plot.UpgradeRequested -= OnUpgradeRequested;
                if (uiById.TryGetValue(plot.PlotId, out var ui))
                {
                    ui.UnlockConfirmed -= OnUnlockConfirmed;
                    ui.UnbindProgression();
                }
            }
            ConstructionVisible = null;
        }
    }
}
