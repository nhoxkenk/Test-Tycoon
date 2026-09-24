using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Farm.UnityAdapters
{
    public sealed class UpgradeItemView : MonoBehaviour
    {
        private Image[] icons;
        private TMP_Text title;
        private TMP_Text description;
        private Button button;
        private UnityAction action;

        public void Bind(Sprite icon, string itemTitle, string itemDescription, string state, bool interactable, Action onClick)
        {
            icons = GetComponentsInChildren<Image>(true);
            foreach (var image in icons)
                if (image.gameObject.name == "Icon") image.sprite = icon;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.gameObject.name == "Title (TMP)") title = text;
                if (text.gameObject.name == "Desc (TMP)") description = text;
            }
            button = GetComponentInChildren<Button>(true);
            action = () => onClick?.Invoke();
            if (button != null) button.onClick.AddListener(action);
            Refresh(icon, itemTitle, itemDescription + "\n" + state, interactable);
        }

        public void Refresh(Sprite icon, string itemTitle, string itemDescription, bool interactable)
        {
            if (title != null) title.text = itemTitle;
            if (description != null) description.text = itemDescription;
            if (icons != null)
                foreach (var image in icons)
                    if (image.gameObject.name == "Icon") image.sprite = icon;
            if (button != null) button.interactable = interactable;
        }

        private void OnDestroy()
        {
            if (button != null && action != null) button.onClick.RemoveListener(action);
        }
    }
}
