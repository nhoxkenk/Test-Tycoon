using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.UnityAdapters
{
    public sealed class ConstructionUpgradeView : MonoBehaviour
    {
        private TMP_Text levelText;
        private TMP_Text costText;
        private Slider slider;
        private Button upgradeButton;
        private Func<int> level;
        private Func<string> cost;
        private Func<bool> canBuy;

        public void BindPlot(Sprite icon, Func<int> getLevel, Func<string> getCost, Func<bool> affordable,
            Action buy, Action close)
        {
            level = getLevel;
            cost = getCost;
            canBuy = affordable;
            var iconImage = FindDeep(transform, "Icon")?.GetComponent<Image>();
            if (iconImage != null) iconImage.sprite = icon;
            slider = GetComponentInChildren<Slider>(true);
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.gameObject.name == "Level (TMP)") levelText = text;
            }
            var coin = FindDeep(transform, "Coin");
            if (coin != null) costText = coin.GetComponentInChildren<TMP_Text>(true);
            upgradeButton = FindDeep(transform, "Upgrade")?.GetComponent<Button>();
            var closeButton = FindDeep(transform, "Close")?.GetComponent<Button>();
            if (upgradeButton != null) upgradeButton.onClick.AddListener(() => buy());
            if (closeButton != null) closeButton.onClick.AddListener(() => close());
            Refresh();
        }

        public void Refresh()
        {
            if (level == null) return;
            var current = level();
            if (levelText != null) levelText.text = "Lv " + current + " / 10";
            if (slider != null) slider.value = Mathf.Clamp01((current - 1f) / 9f);
            if (costText != null) costText.text = current >= 10 ? "MAX" : cost() + " Coin";
            if (upgradeButton != null) upgradeButton.interactable = current < 10 && canBuy();
        }

        public static void ValidatePrefab(GameObject prefab)
        {
            //no-op
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (var i = 0; i < parent.childCount; i++)
            {
                var match = FindDeep(parent.GetChild(i), name);
                if (match != null) return match;
            }
            return null;
        }
    }
}
