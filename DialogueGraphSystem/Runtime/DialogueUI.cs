using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueSystem
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private CanvasGroup dialogueRootCanvasGroup;
        [SerializeField] private CanvasGroup choicePanelCanvasGroup;

        [Header("Dialogue Elements")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private GameObject speakerNamePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image portraitImageFront1;
    [SerializeField] private Image portraitImageFront2;
    [SerializeField] private Image portraitImageFront3;
    [SerializeField] private GameObject portraitPanel;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private CanvasGroup nextIconCanvasGroup;
    [SerializeField] private TextMeshProUGUI conversationPointsText;

        [Header("Dialogue Panel Sprites")]
        [Tooltip("Default dialogue box sprite")]
        [SerializeField] private Sprite defaultDialogueSprite;
        [Tooltip("Optional thought bubble sprite (if not set, uses default)")]
        [SerializeField] private Sprite thoughtDialogueSprite;
        [SerializeField] private Image dialoguePanelImage;

        [Header("Choice Buttons (Pre-existing)")]
        [SerializeField] private List<ChoiceButton> choiceButtons = new List<ChoiceButton>();

        [Header("Settings")]
        [SerializeField] private float choiceFadeDuration = 0.3f;
        [SerializeField] private float dialogueFadeDuration = 0.3f;
        [SerializeField] private float nextIconFadeDuration = 0.25f;
        [SerializeField] private int maxVisibleLines = 4;

        [Header("Text Colors")]
        [Tooltip("Text color for normal dialogue")]
        [SerializeField] private Color dialogueTextColor = new Color(0x5E / 255f, 0x33 / 255f, 0x1B / 255f, 1f); // #5E331B
        [Tooltip("Text color for narration/thoughts")]
        [SerializeField] private Color narrationTextColor = Color.white;

        [Header("Animation")]
        [SerializeField] private PortraitSwayController portraitSwayController;

        private TypewriterEffect typewriter;
        private Coroutine fadeCoroutine;
        private Coroutine dialogueFadeCoroutine;
        private Coroutine speakingScaleCoroutine;
        private Coroutine nextIconFadeCoroutine;
        private int activeChoiceCount;
        private readonly Queue<string> dialogueHistory = new Queue<string>();
        private bool warnedMissingPointsTarget;

        // Speaking scale effect settings
        private const float SPEAKING_SCALE_INTERVAL = 0.1f;
        private const float SPEAKING_SCALE_AMOUNT = 0.002f;
        private RectTransform speakingScaleTarget;
        private Vector3 speakingScaleOriginal;

        // Emotion swap state
        private bool pendingEmotionSwap;
        private Sprite emotionSwapSprite;
        private EmotionSwapTiming emotionSwapTiming;
        private bool emotionSwapTriggered;

        private void Awake()
        {
            typewriter = GetComponent<TypewriterEffect>();
            if (typewriter == null)
            {
                typewriter = gameObject.AddComponent<TypewriterEffect>();
            }

            // Best-effort auto-wire for conversation points display if it was not assigned
            if (conversationPointsText == null)
            {
                conversationPointsText = GetComponentInChildren<TextMeshProUGUI>(true);
                // Auto-assigned successfully
            }

            if (portraitSwayController == null)
            {
                portraitSwayController = GetComponent<PortraitSwayController>();
            }
            if (portraitSwayController == null)
            {
                portraitSwayController = gameObject.AddComponent<PortraitSwayController>();
            }
        }

        public void Show()
        {
            dialoguePanel.SetActive(true);
            if (dialogueRootCanvasGroup != null)
            {
                dialogueRootCanvasGroup.alpha = 1f;
                dialogueRootCanvasGroup.interactable = true;
                dialogueRootCanvasGroup.blocksRaycasts = false; // Don't block raycasts - allow click-to-advance
            }
        }

        public void Hide()
        {
            if (dialogueFadeCoroutine != null)
            {
                StopCoroutine(dialogueFadeCoroutine);
                dialogueFadeCoroutine = null;
            }

            if (dialogueRootCanvasGroup != null)
            {
                dialogueRootCanvasGroup.alpha = 0f;
            }

            dialoguePanel.SetActive(false);
            HideChoices();
            HideContinueIndicator();
            StopPortraitSway();
            StopSpeakingScaleEffect();
        }

        public void FadeIn(Action onComplete = null)
        {
            if (dialogueFadeCoroutine != null)
            {
                StopCoroutine(dialogueFadeCoroutine);
            }
            dialogueFadeCoroutine = StartCoroutine(FadeDialogueRoot(1f, onComplete));
        }

        public void FadeOut(Action onComplete = null)
        {
            if (dialogueFadeCoroutine != null)
            {
                StopCoroutine(dialogueFadeCoroutine);
            }
            dialogueFadeCoroutine = StartCoroutine(FadeDialogueRoot(0f, onComplete));
        }

        private IEnumerator FadeDialogueRoot(float targetAlpha, Action onComplete)
        {
            if (dialogueRootCanvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Disable interaction during fade
            if (targetAlpha < 1f)
            {
                dialogueRootCanvasGroup.interactable = false;
                dialogueRootCanvasGroup.blocksRaycasts = false;
            }

            float startAlpha = dialogueRootCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < dialogueFadeDuration)
            {
                elapsed += Time.deltaTime;
                dialogueRootCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / dialogueFadeDuration);
                yield return null;
            }

            dialogueRootCanvasGroup.alpha = targetAlpha;

            // Enable interaction when fully visible
            if (targetAlpha >= 1f)
            {
                dialogueRootCanvasGroup.interactable = true;
                dialogueRootCanvasGroup.blocksRaycasts = false; // Don't block raycasts - allow click-to-advance
            }

            dialogueFadeCoroutine = null;
            onComplete?.Invoke();
        }

        public void ShowDialogue(
            string speaker,
            string text,
            Sprite portrait,
            bool showPortraitPanel,
            bool showSpeakerName,
            bool isThought,
            bool useEndingPortraitFront,
            int endingPortraitFrontIndex,
            bool useCustomPositioning,
            Vector2 portraitPosition,
            float portraitScale,
            Vector2 portraitAnchor,
            Vector2 portraitSize,
            bool flipPortrait,
            bool emotionSwapEnabled,
            EmotionSwapTiming emotionSwapTimingParam,
            Sprite resolvedEmotionSprite,
            bool swayEnabled,
            SwayPattern swayPattern,
            float swaySpeed,
            float swayIntensity,
            bool sway2Enabled,
            SwayPattern sway2Pattern,
            float sway2Speed,
            float sway2Intensity,
            bool rotationEnabled,
            RotationAxis rotationAxis,
            float rotationMinAngle,
            float rotationMaxAngle,
            float rotationSpeed,
            bool scaleEnabled,
            float scaleMin,
            float scaleMax,
            float scaleSpeed,
            bool speakingScaleEnabled,
            Action onComplete)
        {
            // Sanitize text - fix double spaces and trim
            text = SanitizeDialogueText(text);

            // Ensure root/panel are active before starting coroutines
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            if (dialoguePanel != null && !dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(true);
            }

            // Reset any previous portrait sway so layout starts from a neutral transform
            StopPortraitSway();

            // Set up emotion swap state - sprite resolved externally via DialogueManager.GetEmotionSprite delegate
            emotionSwapSprite = emotionSwapEnabled ? resolvedEmotionSprite : null;

            pendingEmotionSwap = emotionSwapEnabled && emotionSwapSprite != null;
            emotionSwapTiming = emotionSwapTimingParam;
            emotionSwapTriggered = false;

            // Handle immediate emotion swap - swap the portrait before displaying
            if (pendingEmotionSwap && emotionSwapTiming == EmotionSwapTiming.Immediately)
            {
                portrait = emotionSwapSprite;
                emotionSwapTriggered = true;
            }

            // Apply italic formatting if this is a thought
            string formattedText = text;
            if (isThought)
            {
                formattedText = $"<i>{text}</i>";
            }

            // Swap dialogue panel sprite and color based on thought/narration
            if (dialoguePanelImage != null)
            {
                if (isThought)
                {
                    if (thoughtDialogueSprite != null)
                    {
                        dialoguePanelImage.sprite = thoughtDialogueSprite;
                    }
                    // Narration color: #BFAD9C with 75% opacity
                    dialoguePanelImage.color = new Color(0xBF / 255f, 0xAD / 255f, 0x9C / 255f, 0.75f);
                }
                else
                {
                    if (defaultDialogueSprite != null)
                    {
                        dialoguePanelImage.sprite = defaultDialogueSprite;
                    }
                    // Default: white with full opacity
                    dialoguePanelImage.color = Color.white;
                }
            }

            // Set dialogue text color based on narration/thought
            if (dialogueText != null)
            {
                dialogueText.color = isThought ? narrationTextColor : dialogueTextColor;
            }

            // Show/hide speaker name panel
            if (speakerNamePanel != null)
            {
                speakerNamePanel.SetActive(showSpeakerName);
            }

            speakerNameText.text = speaker;

            bool shouldShowPortraitPanel = showPortraitPanel && portrait != null;

            // Select which portrait image to use based on ending portrait front flag and index
            Image activePortraitImage = portraitImage;
            if (useEndingPortraitFront)
            {
                activePortraitImage = endingPortraitFrontIndex switch
                {
                    1 => portraitImageFront1 ?? portraitImage,
                    2 => portraitImageFront2 ?? portraitImage,
                    3 => portraitImageFront3 ?? portraitImage,
                    _ => portraitImage
                };
            }

            if (portraitPanel != null)
            {
                portraitPanel.SetActive(shouldShowPortraitPanel);
            }

            RectTransform portraitPanelRect = portraitPanel != null ? portraitPanel.GetComponent<RectTransform>() : null;
            RectTransform portraitImageRect = activePortraitImage?.GetComponent<RectTransform>();

            RectTransform customTarget = null;
            if (shouldShowPortraitPanel && useCustomPositioning)
            {
                customTarget = portraitPanelRect ?? portraitImageRect;
                if (customTarget != null)
                {
                    // Set anchor and pivot
                    customTarget.anchorMin = portraitAnchor;
                    customTarget.anchorMax = portraitAnchor;
                    customTarget.pivot = portraitAnchor;

                    // Set position
                    customTarget.anchoredPosition = portraitPosition;

                    // Set custom size if specified
                    if (portraitSize.x > 0 && portraitSize.y > 0)
                    {
                        customTarget.sizeDelta = portraitSize;
                    }

                    // Set scale with optional flip
                    Vector3 scale = Vector3.one * portraitScale;
                    scale.x = Mathf.Abs(scale.x) * (flipPortrait ? -1f : 1f);
                    customTarget.localScale = scale;
                }
            }

            RectTransform flipTarget = portraitPanelRect ?? portraitImageRect;
            bool skipFlipAdjustment = customTarget != null && customTarget == flipTarget;
            if (flipTarget != null && (!skipFlipAdjustment || !useCustomPositioning))
            {
                Vector3 existingScale = flipTarget.localScale;
                existingScale.x = Mathf.Abs(existingScale.x) * (flipPortrait ? -1f : 1f);
                flipTarget.localScale = existingScale;
            }

            if (portraitImageRect != null)
            {
                Vector3 imageScale = portraitImageRect.localScale;
                if (portraitPanelRect != null && flipTarget == portraitPanelRect)
                {
                    imageScale.x = Mathf.Abs(imageScale.x);
                }
                else
                {
                    imageScale.x = Mathf.Abs(imageScale.x) * (flipPortrait ? -1f : 1f);
                }
                portraitImageRect.localScale = imageScale;
            }

            // Hide all portrait images first, then show only the active one
            if (portraitImage != null) portraitImage.gameObject.SetActive(false);
            if (portraitImageFront1 != null) portraitImageFront1.gameObject.SetActive(false);
            if (portraitImageFront2 != null) portraitImageFront2.gameObject.SetActive(false);
            if (portraitImageFront3 != null) portraitImageFront3.gameObject.SetActive(false);

            if (activePortraitImage != null)
            {
                if (shouldShowPortraitPanel)
                {
                    activePortraitImage.sprite = portrait;
                    activePortraitImage.gameObject.SetActive(true);
                }
                else
                {
                    activePortraitImage.gameObject.SetActive(false);
                    activePortraitImage.sprite = null;
                }
            }

            ApplyPortraitSway(
                shouldShowPortraitPanel,
                swayEnabled,
                swayPattern,
                swaySpeed,
                swayIntensity,
                sway2Enabled,
                sway2Pattern,
                sway2Speed,
                sway2Intensity,
                rotationEnabled,
                rotationAxis,
                rotationMinAngle,
                rotationMaxAngle,
                rotationSpeed,
                scaleEnabled,
                scaleMin,
                scaleMax,
                scaleSpeed,
                portraitPanelRect,
                portraitImageRect);

            ResetDialogueHistory();

            HideContinueIndicator();

            if (dialogueText != null)
            {
                dialogueText.gameObject.SetActive(true);
                dialogueText.text = string.Empty;
                dialogueText.maxVisibleCharacters = 0;
            }

            string prefix = BuildHistoryPrefix();
            string prefixWithNewline = string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix}\n";
            string displayText = $"{prefixWithNewline}{formattedText}";
            int initialVisibleCharacters = prefixWithNewline.Length;

            typewriter.Stop();

            // Stop any previous speaking scale effect
            StopSpeakingScaleEffect();

            // Start speaking scale effect if enabled, portrait is visible, and not Bonnie
            bool isBonnie = string.Equals(speaker?.Trim(), "Bonnie", System.StringComparison.OrdinalIgnoreCase);
            if (speakingScaleEnabled && shouldShowPortraitPanel && !isBonnie)
            {
                RectTransform scaleTarget = portraitPanelRect ?? portraitImageRect;
                if (scaleTarget != null)
                {
                    StartSpeakingScaleEffect(scaleTarget);
                }
            }

            // Prepare halfway callback for PartialDialogue timing
            Action halfwayCallback = null;
            if (pendingEmotionSwap && emotionSwapTiming == EmotionSwapTiming.PartialDialogue)
            {
                halfwayCallback = () =>
                {
                    if (!emotionSwapTriggered)
                    {
                        SwapPortraitSprite(emotionSwapSprite);
                        emotionSwapTriggered = true;
                    }
                };
            }

            typewriter.StartTyping(dialogueText, displayText, typewriter.DefaultCharactersPerSecond, initialVisibleCharacters, () =>
            {
                StopSpeakingScaleEffect();

                // Handle AfterDialogue emotion swap timing
                if (pendingEmotionSwap && emotionSwapTiming == EmotionSwapTiming.AfterDialogue && !emotionSwapTriggered)
                {
                    SwapPortraitSprite(emotionSwapSprite);
                    emotionSwapTriggered = true;
                }

                AppendToHistory(formattedText);
                onComplete?.Invoke();
            }, halfwayCallback);
        }

        public void CompleteTypewriter()
        {
            StopSpeakingScaleEffect();
            typewriter.Complete();
        }

        public bool TryFastForwardTypewriter()
        {
            bool result = typewriter.TryFastForward();
            if (result)
            {
                StopSpeakingScaleEffect();
            }
            return result;
        }

        public bool IsTyping()
        {
            return typewriter.IsTyping;
        }

        public void ShowContinueIndicator()
        {
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }

            // Fade in the NextIcon
            if (nextIconCanvasGroup != null)
            {
                if (nextIconFadeCoroutine != null)
                {
                    StopCoroutine(nextIconFadeCoroutine);
                }
                nextIconFadeCoroutine = StartCoroutine(FadeNextIcon(1f));
            }
        }

        public void HideContinueIndicator()
        {
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(false);
            }

            // Immediately hide the NextIcon
            if (nextIconCanvasGroup != null)
            {
                if (nextIconFadeCoroutine != null)
                {
                    StopCoroutine(nextIconFadeCoroutine);
                    nextIconFadeCoroutine = null;
                }
                nextIconCanvasGroup.alpha = 0f;
            }
        }

        private IEnumerator FadeNextIcon(float targetAlpha)
        {
            float startAlpha = nextIconCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < nextIconFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / nextIconFadeDuration);
                nextIconCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            nextIconCanvasGroup.alpha = targetAlpha;
            nextIconFadeCoroutine = null;
        }

        public void ShowChoices(List<ChoiceData> choices, Action<int> onChoiceSelected, Action onFadeComplete)
        {
            activeChoiceCount = Mathf.Min(choices.Count, choiceButtons.Count);

            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (i < choices.Count)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].Initialize(i, choices[i].choiceText, onChoiceSelected);
                    choiceButtons[i].SetInteractable(false);
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeInChoices(onFadeComplete));
        }

        private IEnumerator FadeInChoices(Action onComplete)
        {
            choicePanelCanvasGroup.gameObject.SetActive(true);
            choicePanelCanvasGroup.alpha = 0f;
            choicePanelCanvasGroup.interactable = false;
            choicePanelCanvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < choiceFadeDuration)
            {
                elapsed += Time.deltaTime;
                choicePanelCanvasGroup.alpha = Mathf.Clamp01(elapsed / choiceFadeDuration);
                yield return null;
            }

            choicePanelCanvasGroup.alpha = 1f;
            choicePanelCanvasGroup.interactable = true;
            choicePanelCanvasGroup.blocksRaycasts = true;

            for (int i = 0; i < activeChoiceCount; i++)
            {
                choiceButtons[i].SetInteractable(true);
            }

            onComplete?.Invoke();
            fadeCoroutine = null;
        }

        public void HideChoices()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            choicePanelCanvasGroup.alpha = 0f;
            choicePanelCanvasGroup.gameObject.SetActive(false);

            foreach (var button in choiceButtons)
            {
                button.gameObject.SetActive(false);
                button.SetInteractable(false);
            }
        }

        public void ResetDialogueHistory()
        {
            dialogueHistory.Clear();
            dialogueText.text = string.Empty;
        }

        private void ApplyPortraitSway(
            bool shouldShowPortrait,
            bool swayEnabled,
            SwayPattern swayPattern,
            float swaySpeed,
            float swayIntensity,
            bool sway2Enabled,
            SwayPattern sway2Pattern,
            float sway2Speed,
            float sway2Intensity,
            bool rotationEnabled,
            RotationAxis rotationAxis,
            float rotationMinAngle,
            float rotationMaxAngle,
            float rotationSpeed,
            bool scaleEnabled,
            float scaleMin,
            float scaleMax,
            float scaleSpeed,
            RectTransform portraitPanelRect,
            RectTransform portraitImageRect)
        {
            // Sway, sway2, rotation, and scale functionality disabled
            // UI fields are preserved but have no runtime effect
        }

        private string BuildHistoryPrefix()
        {
            if (dialogueHistory.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", dialogueHistory);
        }

        private void AppendToHistory(string text)
        {
            if (maxVisibleLines <= 1)
            {
                return;
            }

            dialogueHistory.Enqueue(text);
            TrimHistory();
        }

        private void TrimHistory()
        {
            int maxHistory = Mathf.Max(0, maxVisibleLines - 1);
            while (dialogueHistory.Count > maxHistory)
            {
                dialogueHistory.Dequeue();
            }
        }

        public void SetConversationPoints(int points)
        {
            if (conversationPointsText == null)
            {
                if (!warnedMissingPointsTarget)
                {
                    Debug.LogWarning("DialogueUI: conversationPointsText is not assigned; cannot display points total.");
                    warnedMissingPointsTarget = true;
                }
                return;
            }
            conversationPointsText.text = $"{points}";
        }

        public void ResetConversationPointsDisplay()
        {
            SetConversationPoints(0);
        }

        /// <summary>
        /// Hides dialogue box and speaker name panel (used for fade transitions).
        /// </summary>
        public void HideDialogueElements()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            if (speakerNamePanel != null)
            {
                speakerNamePanel.SetActive(false);
            }
            if (portraitPanel != null)
            {
                portraitPanel.SetActive(false);
            }
            HideContinueIndicator();
            StopPortraitSway();
            StopSpeakingScaleEffect();
        }

        private void StopPortraitSway()
        {
            portraitSwayController?.StopSway(true);
        }

        /// <summary>
        /// Swaps the current portrait sprite to a new sprite for emotion/expression changes.
        /// </summary>
        private void SwapPortraitSprite(Sprite newSprite)
        {
            if (portraitImage == null || newSprite == null)
                return;

            portraitImage.sprite = newSprite;
        }

        private void StartSpeakingScaleEffect(RectTransform target)
        {
            speakingScaleTarget = target;
            // Capture the original scale when starting the effect
            speakingScaleOriginal = target.localScale;
            speakingScaleCoroutine = StartCoroutine(SpeakingScaleCoroutine());
        }

        private void StopSpeakingScaleEffect()
        {
            if (speakingScaleCoroutine != null)
            {
                StopCoroutine(speakingScaleCoroutine);
                speakingScaleCoroutine = null;
            }

            // Always restore to original scale when stopping
            if (speakingScaleTarget != null)
            {
                speakingScaleTarget.localScale = speakingScaleOriginal;
                speakingScaleTarget = null;
            }
        }

        private IEnumerator SpeakingScaleCoroutine()
        {
            bool isScaledUp = false;

            while (true)
            {
                yield return new WaitForSeconds(SPEAKING_SCALE_INTERVAL);

                if (speakingScaleTarget == null)
                    yield break;

                if (isScaledUp)
                {
                    // Return to original scale
                    speakingScaleTarget.localScale = speakingScaleOriginal;
                }
                else
                {
                    // Scale up by adding the amount (capped to prevent infinite scaling)
                    Vector3 scaledUp = speakingScaleOriginal + Vector3.one * SPEAKING_SCALE_AMOUNT;
                    speakingScaleTarget.localScale = scaledUp;
                }

                isScaledUp = !isScaledUp;
            }
        }

        /// <summary>
        /// Pre-shows the portrait panel with a given sprite without affecting dialogue text.
        /// Used for special effects that need the portrait visible before dialogue starts.
        /// </summary>
        public void PreShowPortrait(Sprite portrait, bool flipPortrait = false)
        {
            if (portrait == null)
                return;

            if (portraitPanel != null)
            {
                portraitPanel.SetActive(true);
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.gameObject.SetActive(true);
            }

            // Apply flip if needed
            RectTransform portraitPanelRect = portraitPanel?.GetComponent<RectTransform>();
            RectTransform portraitImageRect = portraitImage?.GetComponent<RectTransform>();
            RectTransform flipTarget = portraitPanelRect ?? portraitImageRect;

            if (flipTarget != null)
            {
                Vector3 scale = flipTarget.localScale;
                scale.x = Mathf.Abs(scale.x) * (flipPortrait ? -1f : 1f);
                flipTarget.localScale = scale;
            }
        }

        /// <summary>
        /// Pre-shows the portrait panel with a given sprite and initial alpha.
        /// Used for fading in the portrait during screen transitions.
        /// </summary>
        public void PreShowPortraitWithAlpha(Sprite portrait, float alpha, bool flipPortrait = false)
        {
            if (portrait == null)
                return;

            if (portraitPanel != null)
            {
                portraitPanel.SetActive(true);
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.gameObject.SetActive(true);

                // Set initial alpha
                Color c = portraitImage.color;
                c.a = alpha;
                portraitImage.color = c;
            }

            // Apply flip if needed
            RectTransform portraitPanelRect = portraitPanel?.GetComponent<RectTransform>();
            RectTransform portraitImageRect = portraitImage?.GetComponent<RectTransform>();
            RectTransform flipTarget = portraitPanelRect ?? portraitImageRect;

            if (flipTarget != null)
            {
                Vector3 scale = flipTarget.localScale;
                scale.x = Mathf.Abs(scale.x) * (flipPortrait ? -1f : 1f);
                flipTarget.localScale = scale;
            }
        }

        /// <summary>
        /// Sets the alpha of the portrait image. Used for fading the portrait in/out.
        /// </summary>
        public void SetPortraitAlpha(float alpha)
        {
            if (portraitImage != null)
            {
                Color c = portraitImage.color;
                c.a = alpha;
                portraitImage.color = c;
            }
        }

        /// <summary>
        /// Sanitizes dialogue text by fixing common formatting issues.
        /// Uses WebGL-compatible string operations (no Regex).
        /// </summary>
        private string SanitizeDialogueText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Replace multiple consecutive spaces with a single space
            // Using a simple loop approach for WebGL compatibility
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            // Trim leading/trailing whitespace
            text = text.Trim();

            return text;
        }
    }
}
