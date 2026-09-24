using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.UnityAdapters
{
    public sealed class BuildProgressView : MonoBehaviour
    {
        [SerializeField] private Slider progress;
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TMP_Text timeText;

        public void Bind(ResourceConfig resource)
        {
            resourceIcon.sprite = resource.Icon;
            SetRemaining(resource.BuildSeconds, resource.BuildSeconds);
        }

        public void SetRemaining(float remaining, float total)
        {
            progress.value = total <= 0 ? 1 : Mathf.Clamp01(1 - remaining / total);
            timeText.text = Mathf.CeilToInt(Mathf.Max(0, remaining)) + "s";
        }
    }
}
