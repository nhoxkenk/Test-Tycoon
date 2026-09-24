using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.UnityAdapters
{
    public sealed class ConstructionUpgradeView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text buttonCostText;
        [SerializeField] private Slider slider;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

        public event Action BuyClicked;
        public event Action CloseClicked;

        private void Awake()
        {
            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnBuy);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage != null) iconImage.sprite = icon;
        }

        public void Render(int level, string cost, string cashout, bool canBuy)
        {
            if (levelText != null) levelText.text = "Lv " + level + " / 10";
            if (slider != null) slider.value = Mathf.Clamp01((level - 1f) / 9f);
            if (costText != null) costText.text = cashout + " Coin";
            if (buttonCostText != null) buttonCostText.text = level >= 10 ? "MAX" : cost + " Coin";
            if (upgradeButton != null) upgradeButton.interactable = level < 10 && canBuy;
        }

        private void OnBuy() => BuyClicked?.Invoke();
        private void OnClose() => CloseClicked?.Invoke();

        private void OnDestroy()
        {
            if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OnBuy);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnClose);
            BuyClicked = null;
            CloseClicked = null;
        }

        public static void ValidatePrefab(GameObject prefab)
        {
            if (prefab == null) throw new InvalidOperationException("ConstructionUpgradeView prefab is null.");
            var view = prefab.GetComponent<ConstructionUpgradeView>();
            if (view == null) throw new InvalidOperationException("ConstructionUpgradeView prefab is missing ConstructionUpgradeView component.");
            if (view.upgradeButton == null) throw new InvalidOperationException("ConstructionUpgradeView prefab: upgradeButton is not assigned.");
            if (view.closeButton == null) throw new InvalidOperationException("ConstructionUpgradeView prefab: closeButton is not assigned.");
            if (view.costText == null || view.buttonCostText == null)
                throw new InvalidOperationException("ConstructionUpgradeView prefab: cashout or button cost text is not assigned.");
        }
    }
}
