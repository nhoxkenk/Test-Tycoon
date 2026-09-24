using Farm.Farming;
using UnityEngine;

namespace Farm.UnityAdapters
{
    // Unity Update adapter; simulation services remain independent of MonoBehaviour.
    public sealed class FarmTickRunner : MonoBehaviour
    {
        private ConstructionService construction;
        private HarvestService harvest;
        private PlotConstructionController plotPresentation;

        public void Initialize(ConstructionService constructionService, HarvestService harvestService,
            PlotConstructionController plotController)
        {
            construction = constructionService;
            harvest = harvestService;
            plotPresentation = plotController;
            enabled = true;
        }

        private void Update()
        {
            if (construction == null || harvest == null) return;
            construction.Tick(Time.deltaTime);
            harvest.Tick(Time.deltaTime);
            plotPresentation.TickPresentation();
        }

        public void Stop()
        {
            enabled = false;
            construction = null;
            harvest = null;
            plotPresentation = null;
        }
    }
}
