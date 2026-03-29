using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VNWinter.DialogueSystem
{
    [RequireComponent(typeof(RectTransform))]
    public class CharacterHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Scale multiplier when hovering (1 = no scale change).")]
        [SerializeField, Min(1f)] private float hoverScale = 1.08f;

        [Tooltip("Color to lerp toward on hover.")]
        [SerializeField] private Color hoverTint = Color.white;

        [Tooltip("How strongly to blend toward the hover tint (0 = none, 1 = full tint).")]
        [SerializeField, Range(0f, 1f)] private float hoverTintStrength = 0.35f;

        [Tooltip("Additional brightness multiplier while hovering (1 = no change, >1 blends toward white).")]
        [SerializeField, Min(1f)] private float hoverBrightness = 1.15f;

        [Tooltip("Should the hover tint be applied?")]
        [SerializeField] private bool useHoverTint = true;

        [Tooltip("Toggle the outline only when hovering.")]
        [SerializeField] private bool outlineOnlyOnHover = true;

        [Tooltip("Speed of the hover transition (seconds).")]
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.12f;

        private Vector3 baseScale;
        private Graphic targetGraphic;
        private Color baseColor;
        private Outline hoverOutline;
        private bool isHovering;
        private DialogueManager dialogueManager;

        private void Awake()
        {
            baseScale = transform.localScale;

            targetGraphic = GetComponent<Graphic>() ?? GetComponentInChildren<Graphic>();
            if (targetGraphic != null)
            {
                baseColor = targetGraphic.color;
            }

            hoverOutline = GetComponent<Outline>() ?? GetComponentInChildren<Outline>();
            if (hoverOutline != null && outlineOnlyOnHover)
            {
                hoverOutline.enabled = false;
            }

            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        private void Update()
        {
            Vector3 desiredScale = isHovering ? baseScale * hoverScale : baseScale;
            transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * (1f / transitionDuration));

            if (targetGraphic != null)
            {
                Color tintedColor = baseColor;
                if (useHoverTint)
                {
                    tintedColor = Color.Lerp(baseColor, hoverTint, hoverTintStrength);
                }

                float brightnessFactor = Mathf.Clamp01(hoverBrightness - 1f);
                Color brightenedColor = Color.Lerp(tintedColor, Color.white, brightnessFactor);

                Color desiredColor = isHovering ? brightenedColor : baseColor;
                targetGraphic.color = Color.Lerp(targetGraphic.color, desiredColor, Time.deltaTime * (1f / transitionDuration));
            }

            if (hoverOutline != null && outlineOnlyOnHover)
            {
                hoverOutline.enabled = isHovering;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Don't hover if dialogue is active
            if (dialogueManager != null && dialogueManager.IsDialogueActive)
                return;

            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }
    }
}
