using UnityEngine;

namespace Farm.UnityAdapters
{
    [CreateAssetMenu(menuName = "Farm/Resource", fileName = "Resource")]
    public sealed class ResourceConfig : ScriptableObject
    {
        [SerializeField] private string resourceId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private string unlockCost = "10000";
        [SerializeField, Min(0)] private float buildSeconds = 3f;
        [SerializeField, Min(0)] private float harvestSeconds = 2f;
        [SerializeField, Min(0)] private float regrowSeconds = 3f;
        [SerializeField] private string batchSaleValue = "2000";
        [SerializeField] private string productionText = "12.0k/min";
        [Header("Future upgrade data")]
        [SerializeField] private string upgradeCost = "4000";
        [SerializeField, Min(0)] private int profitPercentPerLevel = 10;

        public string ResourceId => resourceId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public string UnlockCost => unlockCost;
        public float BuildSeconds => buildSeconds;
        public float HarvestSeconds => harvestSeconds;
        public float RegrowSeconds => regrowSeconds;
        public string BatchSaleValue => batchSaleValue;
        public string ProductionText => productionText;
        public string UpgradeCost => upgradeCost;
        public int ProfitPercentPerLevel => profitPercentPerLevel;
    }
}
