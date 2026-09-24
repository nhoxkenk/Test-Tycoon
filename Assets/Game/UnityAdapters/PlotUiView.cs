using System;
using UnityEngine;
using UnityEngine.UI;

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

        public event Action<int> UnlockConfirmed;

        public void Initialize(int plotId, ResourceConfig config, Camera gameCamera)
        {
            resource = config;
            var root = new GameObject("World UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
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
            unlock.Bind(config, () => UnlockConfirmed?.Invoke(plotId));
            build.Bind(config);
            information.Bind(config);
            unlock.gameObject.SetActive(false);
            build.gameObject.SetActive(false);
            information.gameObject.SetActive(false);
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

        public void HideUnlock() => unlock.gameObject.SetActive(false);
    }
}
