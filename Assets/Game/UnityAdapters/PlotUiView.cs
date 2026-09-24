using System;
using UnityEngine;
using UnityEngine.UI;
using Farm.Economy;
using Farm.Simulation;

namespace Farm.UnityAdapters
{
    public sealed class PlotUiView : MonoBehaviour
    {
        [SerializeField] private UnlockPanelView unlockPrefab;
        [SerializeField] private BuildProgressView buildPrefab;
        [SerializeField] private InformationView informationPrefab;

        private UnlockPanelView unlock;
        private BuildProgressView build;
        private InformationView information;
        private ResourceConfig resource;
        private Transform worldUiRoot;
        private PlotUpgradeView upgradePresenter;
        private ConstructionUpgradeView upgradeViewInstance;
        private ProgressionService boundProgression;
        private int boundPlotId;

        public event Action<int> UnlockConfirmed;

        public void Initialize(int plotId, ResourceConfig config, Camera gameCamera)
        {
            resource = config;
            var root = new GameObject("World UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            worldUiRoot = root.transform;
            root.transform.localPosition = new Vector3(0, 2.7f, 0);
            root.transform.rotation = gameCamera.transform.rotation;
            root.transform.localScale = Vector3.one * 0.01f;
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(350, 350);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = gameCamera;
            canvas.sortingOrder = 10;

            unlock = Instantiate(unlockPrefab, root.transform);
            build = Instantiate(buildPrefab, root.transform);
            information = Instantiate(informationPrefab, root.transform);
            var informationCanvasGroup = information.GetComponent<CanvasGroup>();
            if (informationCanvasGroup == null) informationCanvasGroup = information.gameObject.AddComponent<CanvasGroup>();
            informationCanvasGroup.interactable = false;
            informationCanvasGroup.blocksRaycasts = false;
            unlock.Bind(config, () => UnlockConfirmed?.Invoke(plotId));
            build.Bind(config);
            information.Bind(config);
            unlock.gameObject.SetActive(false);
            build.gameObject.SetActive(false);
            information.gameObject.SetActive(false);
        }

        public void BindProgression(int plotId, ProgressionService progression, IWalletReader wallet,
            ConstructionUpgradeView upgradePrefab)
        {
            boundProgression = progression;
            boundPlotId = plotId;
            progression.Changed += RefreshEstimatedSale;
            if (upgradePresenter == null && upgradePrefab != null)
            {
                upgradeViewInstance = Instantiate(upgradePrefab, worldUiRoot);
                upgradeViewInstance.transform.localPosition = Vector3.zero;
                upgradeViewInstance.transform.localScale = Vector3.one;
                upgradePresenter = new PlotUpgradeView(upgradeViewInstance, plotId, resource.Icon, progression, wallet);
            }
            RefreshEstimatedSale();
        }

        private void RefreshEstimatedSale()
        {
            if (boundProgression != null && information != null)
                information.SetSaleValue(boundProgression.GetCurrentBatchValue(boundPlotId).ToString());
        }

        public void UnbindProgression()
        {
            if (boundProgression != null) boundProgression.Changed -= RefreshEstimatedSale;
            boundProgression = null;
            upgradePresenter?.Dispose();
            upgradePresenter = null;
            if (upgradeViewInstance != null)
            {
                Destroy(upgradeViewInstance.gameObject);
                upgradeViewInstance = null;
            }
        }

        public void ShowUnlock(bool affordable)
        {
            unlock.gameObject.SetActive(true);
            unlock.SetAffordable(affordable);
        }

        public void ShowBuilding()
        {
            unlock.gameObject.SetActive(false);
            build.gameObject.SetActive(true);
        }

        public void UpdateBuild(float remaining) => build.SetRemaining(remaining, resource.BuildSeconds);

        public void ShowInformation()
        {
            unlock.gameObject.SetActive(false);
            build.gameObject.SetActive(false);
            information.gameObject.SetActive(true);
        }

        public void ShowUpgrade() => upgradePresenter?.Show();
        public void CloseUpgrade() => upgradePresenter?.Close();

        public void HideUnlock() => unlock.gameObject.SetActive(false);

        public void HideInteractiveUi()
        {
            HideUnlock();
            CloseUpgrade();
        }

        private void OnDestroy() => UnbindProgression();
    }
}
