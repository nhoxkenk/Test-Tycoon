using System;
using System.Collections.Generic;
using Farm.Economy;

namespace Farm.Farming
{
    public enum PlotBuildState
    {
        Locked,
        Building,
        Ready
    }

    public sealed class PlotDefinition
    {
        public int Id { get; }
        public Money BuildCost { get; }
        public float BuildSeconds { get; }

        public PlotDefinition(int id, Money buildCost, float buildSeconds)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (buildSeconds < 0 || float.IsNaN(buildSeconds) || float.IsInfinity(buildSeconds))
                throw new ArgumentOutOfRangeException(nameof(buildSeconds));
            Id = id;
            BuildCost = buildCost;
            BuildSeconds = buildSeconds;
        }
    }

    public sealed class PlotSession
    {
        public PlotDefinition Definition { get; }
        public PlotBuildState State { get; internal set; }
        public float RemainingBuildSeconds { get; internal set; }

        internal PlotSession(PlotDefinition definition)
        {
            Definition = definition;
            State = PlotBuildState.Locked;
        }
    }

    public sealed class ConstructionService
    {
        private readonly IWalletTransactions wallet;
        private readonly Dictionary<int, PlotSession> plots = new Dictionary<int, PlotSession>();

        public event Action<PlotSession> PlotChanged;
        public event Action<PlotSession> BuildStarted;
        public event Action<PlotSession> BuildCompleted;

        public ConstructionService(IWalletTransactions wallet, IEnumerable<PlotDefinition> definitions)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            foreach (var definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Plot definition is null.", nameof(definitions));
                plots.Add(definition.Id, new PlotSession(definition));
            }
        }

        public PlotSession GetPlot(int id) => plots.TryGetValue(id, out var plot) ? plot : null;
        public int[] GetPlotIds()
        {
            var ids = new int[plots.Count];
            plots.Keys.CopyTo(ids, 0);
            return ids;
        }

        public bool RestorePlot(int id, PlotBuildState state, float remainingBuildSeconds)
        {
            if (!plots.TryGetValue(id, out var plot) || !Enum.IsDefined(typeof(PlotBuildState), state) ||
                float.IsNaN(remainingBuildSeconds) || float.IsInfinity(remainingBuildSeconds) || remainingBuildSeconds < 0 ||
                (state != PlotBuildState.Building && remainingBuildSeconds != 0) ||
                (state == PlotBuildState.Building && remainingBuildSeconds > plot.Definition.BuildSeconds)) return false;
            plot.State = state;
            plot.RemainingBuildSeconds = state == PlotBuildState.Building ? remainingBuildSeconds : 0;
            PlotChanged?.Invoke(plot);
            return true;
        }

        public bool TryStartBuild(int id)
        {
            if (!plots.TryGetValue(id, out var plot) || plot.State != PlotBuildState.Locked ||
                !wallet.TrySpend(plot.Definition.BuildCost)) return false;

            plot.RemainingBuildSeconds = plot.Definition.BuildSeconds;
            plot.State = PlotBuildState.Building;
            PlotChanged?.Invoke(plot);
            BuildStarted?.Invoke(plot);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            foreach (var plot in plots.Values)
            {
                if (plot.State != PlotBuildState.Building) continue;
                plot.RemainingBuildSeconds = Math.Max(0, plot.RemainingBuildSeconds - deltaTime);
                if (plot.RemainingBuildSeconds > 0) continue;
                plot.State = PlotBuildState.Ready;
                PlotChanged?.Invoke(plot);
                BuildCompleted?.Invoke(plot);
            }
        }
    }
}
