using System;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class TomatoStockView : MonoBehaviour
    {
        [SerializeField] private GameObject tomatoPrefab;
        [SerializeField] private Transform[] stockPoints;

        private GameObject[] tomatoes;

        public Vector3[] StockPositions
        {
            get
            {
                var positions = new Vector3[stockPoints.Length];
                for (var i = 0; i < stockPoints.Length; i++) positions[i] = stockPoints[i].position;
                return positions;
            }
        }

        public void Show(int quantity)
        {
            if (stockPoints == null || quantity < 0 || quantity > stockPoints.Length)
                throw new ArgumentOutOfRangeException(nameof(quantity));
            if (tomatoes == null) tomatoes = new GameObject[stockPoints.Length];

            for (var i = 0; i < stockPoints.Length; i++)
            {
                if (i < quantity && tomatoes[i] == null)
                {
                    if (tomatoPrefab == null || stockPoints[i] == null)
                        throw new InvalidOperationException("Tomato prefab or stock point is missing.");
                    tomatoes[i] = Instantiate(tomatoPrefab, stockPoints[i]);
                    tomatoes[i].transform.localPosition = Vector3.zero;
                    tomatoes[i].transform.localRotation = Quaternion.identity;
                }
                if (tomatoes[i] != null) tomatoes[i].SetActive(i < quantity);
            }
        }
    }
}
