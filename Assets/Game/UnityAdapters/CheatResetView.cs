using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Farm.UnityAdapters
{
    // Debug/cheat button that deletes all save data and reloads the scene.
    public sealed class CheatResetView : MonoBehaviour
    {
        private System.Action beforeReset;

        public void Initialize(Canvas canvas, System.Action beforeReset)
        {
            this.beforeReset = beforeReset;
            var btnObj = new GameObject("CheatReset", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            btnObj.transform.SetParent(canvas.transform, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(10f, 10f);
            rect.sizeDelta = new Vector2(120f, 40f);

            var image = btnObj.GetComponent<Image>();
            image.color = new Color(0.8f, 0.2f, 0.2f, 0.85f);

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(btnObj.transform, false);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObj.GetComponent<TMP_Text>();
            label.text = "RESET";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            btnObj.GetComponent<Button>().onClick.AddListener(OnReset);
        }

        private void OnReset()
        {
            beforeReset?.Invoke();
            // Delete save files
            var savePath = Path.Combine(Application.persistentDataPath, "farm-progress.json");
            try
            {
                if (File.Exists(savePath)) File.Delete(savePath);
                if (File.Exists(savePath + ".bak")) File.Delete(savePath + ".bak");
                if (File.Exists(savePath + ".tmp")) File.Delete(savePath + ".tmp");
                Debug.Log("[Cheat] Save data deleted.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Cheat] Failed to delete save: " + e.Message);
            }

            // Reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
