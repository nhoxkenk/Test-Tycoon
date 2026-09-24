using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class ConstructionView : MonoBehaviour
    {
        [SerializeField] private Transform harvestPoint;
        [SerializeField] private TomatoStockView stockView;

        public Transform HarvestPoint => harvestPoint;
        public Vector3[] StockPositions => stockView.StockPositions;
        public void ShowStock(int quantity) => stockView.Show(quantity);
    }
}
