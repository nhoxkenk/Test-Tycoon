using System;
using Farm.Economy;
using Farm.Farming;
using Farm.Simulation;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class ProgressPersistenceController : MonoBehaviour
    {
        private IProgressStore store;
        private Wallet wallet;
        private ConstructionService construction;
        private HarvestService harvest;
        private ProgressionService progression;
        private float saveIn;
        private float buildingSaveIn;
        private bool dirty;
        private bool canSave;

        public void Initialize(IProgressStore progressStore, Wallet boundWallet, ConstructionService boundConstruction,
            HarvestService boundHarvest, ProgressionService boundProgression, bool allowSave)
        {
            store = progressStore;
            wallet = boundWallet;
            construction = boundConstruction;
            harvest = boundHarvest;
            progression = boundProgression;
            canSave = allowSave;
            if (!canSave) return;
            wallet.BalanceChanged += OnBalanceChanged;
            construction.PlotChanged += OnPlotChanged;
            progression.Changed += MarkDirty;
            enabled = true;
        }

        private void OnBalanceChanged(Money _) => MarkDirty();
        private void OnPlotChanged(PlotSession _) => MarkDirty();
        private void MarkDirty() { dirty = true; saveIn = .35f; }

        private void Update()
        {
            if (!canSave) return;
            var building = false;
            foreach (var id in construction.GetPlotIds())
                if (construction.GetPlot(id).State == PlotBuildState.Building) { building = true; break; }
            if (building)
            {
                buildingSaveIn -= Time.unscaledDeltaTime;
                if (buildingSaveIn <= 0) { MarkDirty(); buildingSaveIn = 1f; }
            }
            if (!dirty) return;
            saveIn -= Time.unscaledDeltaTime;
            if (saveIn <= 0) Flush();
        }

        private void OnApplicationPause(bool paused) { if (paused) FlushCurrentState(); }
        private void OnApplicationQuit() => FlushCurrentState();

        private void FlushCurrentState()
        {
            if (canSave) dirty = true;
            Flush();
        }

        public void Flush()
        {
            if (!canSave || !dirty) return;
            try
            {
                store.Save(ProgressSnapshotFactory.Capture(wallet, construction, harvest, progression));
                dirty = false;
            }
            catch (Exception error) { Debug.LogException(error, this); }
        }

        private void OnDestroy()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            if (construction != null) construction.PlotChanged -= OnPlotChanged;
            if (progression != null) progression.Changed -= MarkDirty;
            FlushCurrentState();
        }
    }
}
