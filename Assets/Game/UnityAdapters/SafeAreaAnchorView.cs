using UnityEngine;

namespace Farm.UnityAdapters
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaAnchorView : MonoBehaviour
    {
        private RectTransform[] safeRoots;
        private Rect lastSafeArea;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            safeRoots = new[] { FindRoot("Top"), FindRoot("Bot") };
            ApplyIfChanged(true);
        }

        private void Update() => ApplyIfChanged(false);

        private RectTransform FindRoot(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child as RectTransform : null;
        }

        private void ApplyIfChanged(bool force)
        {
            var area = Screen.safeArea;
            if (!force && area == lastSafeArea && Screen.width == lastWidth && Screen.height == lastHeight) return;
            lastSafeArea = area;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var min = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            var max = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            foreach (var root in safeRoots)
            {
                if (root == null) continue;
                root.anchorMin = min;
                root.anchorMax = max;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }
        }
    }
}
