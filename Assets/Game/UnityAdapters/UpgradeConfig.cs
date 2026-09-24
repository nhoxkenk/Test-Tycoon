using UnityEngine;

namespace Farm.UnityAdapters
{
    [CreateAssetMenu(menuName = "Farm/Upgrade", fileName = "Upgrade")]
    public sealed class UpgradeConfig : ScriptableObject
    {
        public enum Effect { AddCustomers, DoubleAllPlots, DoublePlot }
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Effect effect;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private int plotId;
        [SerializeField] private string coinCost = "10000";
        [SerializeField] private Sprite icon;
        public string Id => id;
        public string Title => title;
        public string Description => description;
        public Effect UpgradeEffect => effect;
        public int Amount => amount;
        public int PlotId => plotId;
        public string CoinCost => coinCost;
        public Sprite Icon => icon;
    }
}
