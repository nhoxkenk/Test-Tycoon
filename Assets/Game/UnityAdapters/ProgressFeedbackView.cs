using System;
using System.Collections.Generic;
using Farm.Actors;
using Farm.Farming;
using Farm.Simulation;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class ProgressFeedbackView : MonoBehaviour, IDisposable
    {
        private ConstructionService construction;
        private MarketSaleService market;
        private ActorSceneInstaller actors;
        private readonly Dictionary<int, PlotSlotView> plots = new Dictionary<int, PlotSlotView>();
        private GameObject payPrefab;
        private GameObject buildDonePrefab;

        public void Initialize(ConstructionService constructionService, MarketSaleService marketService,
            ActorSceneInstaller actorInstaller, IEnumerable<PlotSlotView> plotViews,
            GameObject payEffectPrefab, GameObject buildDoneEffectPrefab)
        {
            construction = constructionService ?? throw new ArgumentNullException(nameof(constructionService));
            market = marketService ?? throw new ArgumentNullException(nameof(marketService));
            actors = actorInstaller ?? throw new ArgumentNullException(nameof(actorInstaller));
            foreach (var plot in plotViews) plots.Add(plot.PlotId, plot);
            payPrefab = payEffectPrefab;
            buildDonePrefab = buildDoneEffectPrefab;
            construction.BuildStarted += OnBuildStarted;
            construction.BuildCompleted += OnBuildCompleted;
            market.SaleCommitted += OnSaleCommitted;
        }

        private void OnBuildStarted(PlotSession plot)
        {
            if (plots.TryGetValue(plot.Definition.Id, out var view)) Play(payPrefab, view.transform.position + Vector3.up);
        }

        private void OnBuildCompleted(PlotSession plot)
        {
            if (plots.TryGetValue(plot.Definition.Id, out var view)) Play(buildDonePrefab, view.transform.position + Vector3.up);
        }

        private void OnSaleCommitted(HarvestBatch batch, int customerId)
        {
            var position = Vector3.zero;
            if (!actors.TryGetCustomerPosition(customerId, out position) && plots.TryGetValue(batch.PlotId, out var plot))
                position = plot.transform.position + Vector3.up;
            Play(payPrefab, position);
        }

        private static void Play(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;
            var instance = Instantiate(prefab, position, prefab.transform.rotation);
            var lifetime = 0f;
            foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Play(true);
                var main = particle.main;
                if (main.loop) continue;
                lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            }
            Destroy(instance, Mathf.Clamp(lifetime + .5f, 2f, 10f));
        }

        public void Dispose()
        {
            if (construction != null)
            {
                construction.BuildStarted -= OnBuildStarted;
                construction.BuildCompleted -= OnBuildCompleted;
            }
            if (market != null) market.SaleCommitted -= OnSaleCommitted;
            construction = null;
            market = null;
            actors = null;
            plots.Clear();
        }

        private void OnDestroy() => Dispose();
    }
}
