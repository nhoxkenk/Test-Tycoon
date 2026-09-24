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
        private bool ready;

        public int PlotId => plotId;
        public ResourceConfig Resource => resource;
        public Transform HarvestPoint => construction != null ? construction.HarvestPoint : null;
        public Vector3[] StockPositions => construction != null ? construction.StockPositions : null;
        public event Action<int> BuildRequested;
        public event Action<PlotSlotView> ConstructionVisible;

        private void Awake()
        {
            if (boxPrefab == null || constructionPrefab == null)
            {
                Debug.LogError("Plot prefabs are missing.", this);
                enabled = false;
                return;
            }
            box = Instantiate(boxPrefab, transform);
            box.Clicked += OnBoxClicked;
        }

        public void BeginBuild()
        {
            if (box == null) return;
            box.Clicked -= OnBoxClicked;
            box.PlayOpen();
        }

        public void MarkReady()
        {
            ready = true;
            TryShowConstruction();
        }

        public void ShowStock(int quantity) => construction?.ShowStock(quantity);

        private void Update() => TryShowConstruction();

        private void TryShowConstruction()
        {
            if (!ready || box == null || box.IsOpening) return;
            box.Clicked -= OnBoxClicked;
            Destroy(box.gameObject);
            box = null;
            construction = Instantiate(constructionPrefab, transform);
            construction.ShowStock(3);
            ConstructionVisible?.Invoke(this);
        }

        private void OnBoxClicked() => BuildRequested?.Invoke(plotId);

        private void OnDestroy()
        {
            if (box != null) box.Clicked -= OnBoxClicked;
        }
    }
}
