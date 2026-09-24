using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class BoxView : MonoBehaviour
    {
        private Animation boxAnimation;

        public bool IsOpening => boxAnimation != null && boxAnimation.IsPlaying("BoxOpen");

        private void Awake() => boxAnimation = GetComponentInChildren<Animation>(true);
        public void PlayOpen()
        {
            if (boxAnimation != null && boxAnimation.GetClip("BoxOpen") != null)
                boxAnimation.Play("BoxOpen");
        }
    }
}
