using UnityEngine;
using UnityEngine.UI;

namespace VNWinter.DialogueSystem
{
    /// <summary>Utility component that flips its Image/RectTransform horizontally without relying on animation.</summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class FlipImage : MonoBehaviour
    {
        [SerializeField] private bool startFlipped;

        private Vector3 baseScale;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            baseScale = rectTransform.localScale;
            ApplyFlip(startFlipped);
        }

        /// <summary>Flip the image horizontally.</summary>
        public void ApplyFlip(bool flipped)
        {
            if (rectTransform == null)
                return;

            Vector3 newScale = baseScale;
            newScale.x = Mathf.Abs(newScale.x) * (flipped ? -1f : 1f);
            rectTransform.localScale = newScale;
        }

        /// <summary>Update the cached base scale (call this if parent changes the scale).</summary>
        public void UpdateBaseScale(Vector3 newBaseScale)
        {
            baseScale = newBaseScale;
        }

        /// <summary>Refresh the cached base scale from the current transform.</summary>
        public void SyncBaseScaleFromTransform()
        {
            if (rectTransform == null)
                return;

            baseScale = rectTransform.localScale;
        }
    }
}
