using System;
using UnityEngine.EventSystems;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class PlotSlotView : MonoBehaviour
    {
        [SerializeField] private int plotId;
        [SerializeField] private BoxView boxPrefab;
        [SerializeField] private ConstructionView constructionPrefab;
        [SerializeField] private ResourceConfig resource;

        private BoxView box;
        private ConstructionView construction;
        private bool ready;
        private bool building;
        private Camera interactionCamera;

        public int PlotId => plotId;
        public ResourceConfig Resource => resource;
        public Transform HarvestPoint => construction != null ? construction.HarvestPoint : null;
        public Vector3[] StockPositions => construction != null ? construction.StockPositions : null;
        public event Action<int> BuildRequested;
        public event Action<PlotSlotView> ConstructionVisible;
        public event Action<PlotSlotView> UpgradeRequested;

        private void Awake()
        {
            if (GetComponent<Collider>() == null)
            {
                var hitArea = gameObject.AddComponent<BoxCollider>();
                hitArea.center = new Vector3(0, .75f, 0);
                hitArea.size = new Vector3(2.5f, 1.5f, 2.5f);
            }
            if (boxPrefab == null || constructionPrefab == null)
            {
                Debug.LogError("Plot prefabs are missing.", this);
                enabled = false;
                return;
            }
            box = Instantiate(boxPrefab, transform);
        }

        public void BindInteractionCamera(Camera camera) => interactionCamera = camera;

        public void BeginBuild()
        {
            if (box == null) return;
            building = true;
            box.PlayOpen();
        }

        public void MarkReady()
        {
            building = false;
            ready = true;
            TryShowConstruction();
        }

        public void ShowStock(int quantity) => construction?.ShowStock(quantity);

        private void Update()
        {
            TryShowConstruction();
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began) RouteScreenClick(touch.position, touch.fingerId);
            }
        }

        private void TryShowConstruction()
        {
            if (!ready || box == null || box.IsOpening) return;
            Destroy(box.gameObject);
            box = null;
            construction = Instantiate(constructionPrefab, transform);
            construction.ShowStock(3);
            ConstructionVisible?.Invoke(this);
        }

        private void OnMouseDown()
        {
            if (Input.touchCount > 0) return;
            RouteScreenClick(Input.mousePosition, -1);
        }

        // The root collider receives clicks for both the box and ready construction.
        // Raycast again so UI hits block the plot and nested colliders cannot bypass routing.
        private bool RouteScreenClick(Vector2 screenPosition, int pointerId)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var pointer = new PointerEventData(eventSystem) { position = screenPosition, pointerId = pointerId };
                var uiHits = new System.Collections.Generic.List<RaycastResult>();
                eventSystem.RaycastAll(pointer, uiHits);
                if (uiHits.Count > 0) return false;
            }
            var camera = interactionCamera != null ? interactionCamera : Camera.main;
            if (camera == null || !Physics.Raycast(camera.ScreenPointToRay(screenPosition), out var hit, 1000f)) return false;
            if (hit.collider.GetComponentInParent<PlotSlotView>() != this) return false;

            if (ready) UpgradeRequested?.Invoke(this);
            else if (!building) BuildRequested?.Invoke(plotId);
            else return false;
            return true;
        }
    }
}
