using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.UnityAdapters
{
    public sealed class InformationView : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TMP_Text saleText;
        [SerializeField] private TMP_Text productionText;

        public void Bind(ResourceConfig resource)
        {
            resourceIcon.sprite = resource.Icon;
            saleText.text = resource.BatchSaleValue;
            productionText.text = resource.ProductionText;
        }

        public void SetSaleValue(string value) => saleText.text = value;
    }
}
