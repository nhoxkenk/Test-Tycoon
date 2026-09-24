using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Farm.UnityAdapters
{
    public sealed class BoxView : MonoBehaviour
    {
        private Animation boxAnimation;

        public event Action Clicked;
        public bool IsOpening => boxAnimation != null && boxAnimation.IsPlaying("BoxOpen");

        private void Awake() => boxAnimation = GetComponentInChildren<Animation>(true);
        private void OnMouseDown()
        {
            if (EventSystem.current != null)
            {
                var pointerId = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
                if (EventSystem.current.IsPointerOverGameObject(pointerId)) return;
            }
            Clicked?.Invoke();
        }

        public void PlayOpen()
        {
            if (boxAnimation != null && boxAnimation.GetClip("BoxOpen") != null)
                boxAnimation.Play("BoxOpen");
        }
    }
}
