using System;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class PlotSlotView : MonoBehaviour
    {
        [SerializeField] private int plotId;
        [SerializeField] private BoxView boxPrefab;
        [SerializeField] private ConstructionView constructionPrefab;
        [SerializeField] private ResourceConfig resource;

        private BoxView box;
        private ConstructionView construction;
        private Collider plotCollider;
        private bool ready;
        private bool building;

        public int PlotId => plotId;
        public ResourceConfig Resource => resource;
        public Transform HarvestPoint => construction != null ? construction.HarvestPoint : null;
        public Vector3[] StockPositions => construction != null ? construction.StockPositions : null;
        public event Action<int> BuildRequested;
        public event Action<PlotSlotView> ConstructionVisible;
        public event Action<PlotSlotView> UpgradeRequested;

        private void Awake()
        {
            plotCollider = GetComponent<Collider>();
            if (plotCollider == null)
            {
                var hitArea = gameObject.AddComponent<BoxCollider>();
                hitArea.center = new Vector3(0, .75f, 0);
                hitArea.size = new Vector3(2.5f, 1.5f, 2.5f);
                plotCollider = hitArea;
            }
            if (boxPrefab == null || constructionPrefab == null)
            {
                Debug.LogError("Plot prefabs are missing.", this);
                enabled = false;
                return;
            }
            box = Instantiate(boxPrefab, transform);
        }

        public void BeginBuild()
        {
            if (box == null) return;
            building = true;
            box.PlayOpen();
        }

        public void MarkReady()
        {
            building = false;
            ready = true;
            if (plotCollider != null) plotCollider.enabled = false;
            TryShowConstruction();
        }

        public void ShowStock(int quantity) => construction?.ShowStock(quantity);

        private void Update()
        {
            TryShowConstruction();
        }

        private void TryShowConstruction()
        {
            if (!ready || box == null || box.IsOpening) return;
            Destroy(box.gameObject);
            box = null;
            construction = Instantiate(constructionPrefab, transform);
            var hitArea = construction.gameObject.AddComponent<BoxCollider>();
            hitArea.center = new Vector3(0, .5f, 0);
            hitArea.size = new Vector3(1.5f, 1.5f, 1.5f);
            construction.ShowStock(3);
            ConstructionVisible?.Invoke(this);
        }

        public void RequestInteraction()
        {
            if (ready) UpgradeRequested?.Invoke(this);
            else if (!building) BuildRequested?.Invoke(plotId);
        }
    }
}
