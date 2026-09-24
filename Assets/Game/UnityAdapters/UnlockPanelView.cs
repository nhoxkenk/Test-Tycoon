using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Farm.UnityAdapters
{
    public sealed class UnlockPanelView : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button unlockButton;

        private UnityAction clickHandler;

        public void Bind(ResourceConfig resource, Action onUnlock)
        {
            if (clickHandler != null) unlockButton.onClick.RemoveListener(clickHandler);
            resourceIcon.sprite = resource.Icon;
            title.text = "Unlock " + resource.DisplayName;
            description.text = resource.ResourceId;
            costText.text = resource.UnlockCost;
            clickHandler = () => onUnlock?.Invoke();
            unlockButton.onClick.AddListener(clickHandler);
        }

        public void SetAffordable(bool affordable) => unlockButton.interactable = affordable;

        private void OnDestroy()
        {
            if (clickHandler != null) unlockButton.onClick.RemoveListener(clickHandler);
        }
    }
}
