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

        public void Initialize(Canvas canvas, GameObject sectionPrefab, GameObject itemPrefab,
            UpgradeConfig[] configs, ProgressionService service, IWalletReader reader)
        {
            progression = service;
            wallet = reader;
            var navTransform = canvas.transform.Find("Bot/MainBotBarView/BotBarItem/Button");
            navButton = navTransform != null ? navTransform.GetComponent<Button>() : null;
            if (navButton == null) throw new MissingReferenceException("MainView bottom Upgrade button is missing.");
            navButton.onClick.AddListener(Open);
            var navIcon = FindDeep(navTransform, "IconImage")?.GetComponent<Image>();
            var menuConfig = System.Array.Find(configs, config => config != null && config.Id == "income_all_x2");
            if (navIcon != null && menuConfig != null) navIcon.sprite = menuConfig.Icon;
            panel = Instantiate(sectionPrefab, canvas.transform);
            panel.name = "Upgrade Section";
            var blocker = panel.GetComponent<Image>();
            if (blocker == null) blocker = panel.AddComponent<Image>();
            blocker.color = new Color(0, 0, 0, .62f);
            blocker.raycastTarget = true;
            var sectionCanvas = panel.GetComponent<Canvas>();
            if (sectionCanvas != null) sectionCanvas.sortingOrder = 100;
            var close = FindDeep(panel.transform, "Close")?.GetComponent<Button>();
            if (close != null) close.onClick.AddListener(Close);
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
                if (item == null) item = row.AddComponent<UpgradeItemView>();
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

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var match = FindDeep(root.GetChild(i), name);
                if (match != null) return match;
            }
            return null;
        }

        public static void ValidatePrefabs(Canvas canvas, GameObject sectionPrefab, GameObject upgradeSectionPrefab)
        {
            //no-op
        }

        private void OnDestroy()
        {
            if (navButton != null) navButton.onClick.RemoveListener(Open);
            if (progression != null) progression.Changed -= Refresh;
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
        }
    }
}
