using Farm.Economy;
using Farm.Simulation;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class PlotUpgradeView : MonoBehaviour
    {
        private ProgressionService progression;
        private IWalletReader boundWallet;
        private int plotId;
        private ConstructionUpgradeView view;
        private GameObject viewObject;

        public void Initialize(Canvas canvas, GameObject prefab, int id, ResourceConfig resource,
            ProgressionService service, IWalletReader wallet)
        {
            plotId = id;
            progression = service;
            boundWallet = wallet;
            viewObject = Instantiate(prefab.gameObject, canvas.transform);
            viewObject.name = "Plot Upgrade " + id;
            var blocker = viewObject.GetComponent<UnityEngine.UI.Image>();
            if (blocker == null) blocker = viewObject.AddComponent<UnityEngine.UI.Image>();
            blocker.color = new Color(0, 0, 0, 0);
            blocker.raycastTarget = true;
            view = viewObject.GetComponent<ConstructionUpgradeView>();
            if (view == null) view = viewObject.AddComponent<ConstructionUpgradeView>();
            var canvasComponent = viewObject.GetComponent<Canvas>();
            if (canvasComponent != null) canvasComponent.sortingOrder = 50;
            view.BindPlot(resource.Icon, () => progression.GetLevel(plotId),
                () => progression.GetNextLevelCost(plotId).ToString(),
                () => progression.CanUpgradePlot(plotId), OnBuy, Close);
            progression.Changed += Refresh;
            wallet.BalanceChanged += OnBalanceChanged;
            viewObject.SetActive(false);
            Refresh();
        }

        public void Show(bool visible)
        {
            if (visible)
                foreach (var other in FindObjectsOfType<PlotUpgradeView>())
                    if (other != this) other.Close();
            if (viewObject != null) viewObject.SetActive(visible);
        }

        private void OnBuy()
        {
            progression.TryUpgradePlot(plotId);
            Refresh();
        }

        private void Refresh() => view?.Refresh();
        private void OnBalanceChanged(Money _) => Refresh();
        private void Close() { if (viewObject != null) viewObject.SetActive(false); }

        private void OnDestroy()
        {
            if (progression != null) progression.Changed -= Refresh;
            if (boundWallet != null) boundWallet.BalanceChanged -= OnBalanceChanged;
            boundWallet = null;
            if (viewObject != null) Destroy(viewObject);
        }
    }
}
