using System;
using System.Collections.Generic;
using Farm.Economy;
using Farm.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.UnityAdapters
{
    public sealed class UpgradeMenuView : MonoBehaviour
    {
        private sealed class Entry
        {
            public UpgradeConfig Config;
            public UpgradeDefinition Definition;
            public UpgradeItemView View;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private ProgressionService progression;
        private IWalletReader wallet;
        private GameObject panel;
        private Button navButton;

        public void Initialize(Canvas canvas, Button upgradeNavButton, GameObject sectionPrefab,
            GameObject itemPrefab, UpgradeConfig[] configs, ProgressionService service, IWalletReader reader)
        {
            progression = service;
            wallet = reader;
            navButton = upgradeNavButton;
            if (navButton == null) throw new MissingReferenceException("Upgrade navigation button is missing.");
            navButton.onClick.AddListener(Open);

            var menuConfig = Array.Find(configs, config => config != null && config.Id == "income_all_x2");
            var navIcon = navButton.GetComponentInChildren<Image>();
            if (navIcon != null && menuConfig != null) navIcon.sprite = menuConfig.Icon;

            panel = Instantiate(sectionPrefab, canvas.transform);
            panel.name = "Upgrade Section";
            var blocker = panel.GetComponent<Image>();
            if (blocker != null)
            {
                blocker.color = new Color(0, 0, 0, .62f);
                blocker.raycastTarget = true;
            }
            var sectionCanvas = panel.GetComponent<Canvas>();
            if (sectionCanvas != null) sectionCanvas.sortingOrder = 100;
            var close = panel.GetComponentInChildren<Button>(true);
            if (close != null && close.gameObject.name == "Close") close.onClick.AddListener(Close);
            else
            {
                // Fallback: search for a button named Close
                foreach (var btn in panel.GetComponentsInChildren<Button>(true))
                    if (btn.gameObject.name == "Close") { btn.onClick.AddListener(Close); break; }
            }
            var content = panel.transform.Find("Scroll View/Viewport/Content");
            if (content == null) content = FindContent(panel.transform);
            if (content == null) throw new MissingReferenceException("UpgradeView prefab has no scroll Content transform.");

            foreach (var config in configs)
            {
                if (config == null) continue;
                var definition = progression.GetUpgrade(config.Id);
                var row = Instantiate(itemPrefab, content, false);
                row.name = config.Id;
                var item = row.GetComponent<UpgradeItemView>();
                if (item == null) throw new MissingReferenceException("UpgradeItemView prefab is missing UpgradeItemView component on " + config.Id);
                var entry = new Entry { Config = config, Definition = definition, View = item };
                item.Bind(config.Icon, config.Title, config.Description, "", false, () => progression.TryPurchase(config.Id));
                entries.Add(entry);
            }

            progression.Changed += Refresh;
            wallet.BalanceChanged += OnBalanceChanged;
            panel.SetActive(false);
            Refresh();
        }

        private void Open()
        {
            panel.SetActive(true);
            Refresh();
        }

        private void Close() => panel.SetActive(false);
        private void OnBalanceChanged(Money _) => Refresh();

        private void Refresh()
        {
            if (progression == null) return;
            foreach (var entry in entries)
            {
                var definition = entry.Definition;
                var bought = progression.IsPurchased(entry.Config.Id);
                var status = bought ? "Owned" :
                    definition.Kind == UpgradeKind.PlotIncome && !progression.IsPlotReady(entry.Config.PlotId)
                        ? "Build plot " + entry.Config.PlotId + " first"
                        : wallet.GetBalance(definition.Cost.Currency).Amount < definition.Cost.Amount
                            ? "Insufficient funds: " + definition.Cost + " Coin"
                            : "Buy: " + definition.Cost + " Coin";
                entry.View.Refresh(entry.Config.Icon, entry.Config.Title, entry.Config.Description + "\n" + status,
                    !bought && progression.CanPurchase(entry.Config.Id));
            }
        }

        private void Update()
        {
            if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private static Transform FindContent(Transform root)
        {
            foreach (var group in root.GetComponentsInChildren<VerticalLayoutGroup>(true))
                if (group.gameObject.name == "Content") return group.transform;
            return null;
        }

        private void OnDestroy()
        {
            if (navButton != null) navButton.onClick.RemoveListener(Open);
            if (progression != null) progression.Changed -= Refresh;
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
        }
    }
}
