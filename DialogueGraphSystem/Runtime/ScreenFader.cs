using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VNWinter.DialogueSystem
{
    /// <summary>
    /// Singleton component that manages full-screen fade effects.
    /// Automatically creates a fullscreen canvas overlay for fading to/from colors.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader instance;

        /// <summary>
        /// Singleton instance. Auto-creates on first access.
        /// </summary>
        public static ScreenFader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ScreenFader");
                    instance = go.AddComponent<ScreenFader>();
                    DontDestroyOnLoad(go);
                    instance.Initialize();
                }
                return instance;
            }
        }

        private Canvas fadeCanvas;
        private Image fadeImage;
        private Coroutine currentFadeCoroutine;

        /// <summary>
        /// Event fired during FadeIn with progress (0-1). Subscribe to react at specific fade points.
        /// </summary>
        public event Action<float> OnFadeInProgress;

        private void Awake()
        {
            // If this instance wasn't created via Instance property, make it the singleton
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Clean up to prevent inspector errors
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }

            // Destroy canvas and image cleanly
            if (fadeImage != null)
            {
                Destroy(fadeImage.gameObject);
                fadeImage = null;
            }

            if (fadeCanvas != null && fadeCanvas.gameObject != null)
            {
                Destroy(fadeCanvas.gameObject);
                fadeCanvas = null;
            }

            // Clear singleton reference if this was the instance
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Initializes the fullscreen canvas and fade image.
        /// </summary>
        private void Initialize()
        {
            // Create canvas
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform);

            fadeCanvas = canvasGO.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9997; // Above most things, but below 9999

            // Add canvas scaler
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Add graphic raycaster
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create fullscreen image
            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform);

            fadeImage = imageGO.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0); // Start transparent

            // Configure as fullscreen
            var rectTransform = fadeImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            // Don't block raycasts - transparency should allow interaction
            fadeImage.raycastTarget = false;

            Debug.Log("ScreenFader: Initialized fullscreen fade overlay");
        }

        /// <summary>
        /// Fades to the specified color (alpha goes from 0 to 1).
        /// </summary>
        /// <param name="targetColor">Color to fade to</param>
        /// <param name="duration">Duration of fade in seconds</param>
        /// <param name="onComplete">Callback when fade completes</param>
        public void FadeOut(Color targetColor, float duration, Action onComplete = null)
        {
            if (fadeImage == null)
            {
                Debug.LogError("ScreenFader: fadeImage is null, cannot fade out");
                onComplete?.Invoke();
                return;
            }

            // Stop any existing fade
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            // Set the target color (with alpha 0 initially)
            Color startColor = targetColor;
            startColor.a = 0f;
            fadeImage.color = startColor;

            // Fade alpha from 0 to 1
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, targetColor, duration, onComplete));
        }

        /// <summary>
        /// Fades from current color to transparent (alpha goes from 1 to 0).
        /// </summary>
        /// <param name="duration">Duration of fade in seconds</param>
        /// <param name="onComplete">Callback when fade completes</param>
        public void FadeIn(float duration, Action onComplete = null)
        {
            FadeIn(duration, null, onComplete);
        }

        /// <summary>
        /// Fades from current color to transparent (alpha goes from 1 to 0).
        /// </summary>
        /// <param name="duration">Duration of fade in seconds</param>
        /// <param name="onProgress">Callback with progress (0-1) called each frame</param>
        /// <param name="onComplete">Callback when fade completes</param>
        public void FadeIn(float duration, Action<float> onProgress, Action onComplete = null)
        {
            if (fadeImage == null)
            {
                Debug.LogError("ScreenFader: fadeImage is null, cannot fade in");
                onComplete?.Invoke();
                return;
            }

            // Stop any existing fade
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            // Ensure alpha is 1 at start
            Color currentColor = fadeImage.color;
            currentColor.a = 1f;
            fadeImage.color = currentColor;

            // Combine the local onProgress callback with the public event
            Action<float> combinedProgress = (progress) =>
            {
                onProgress?.Invoke(progress);
                OnFadeInProgress?.Invoke(progress);
            };

            // Fade alpha from 1 to 0
            currentFadeCoroutine = StartCoroutine(FadeCoroutineWithProgress(1f, 0f, currentColor, duration, combinedProgress, onComplete));
        }

        /// <summary>
        /// Coroutine that performs the actual fade animation.
        /// </summary>
        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, Color baseColor, float duration, Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                fadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }

            // Ensure final alpha is exact
            fadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, endAlpha);

            currentFadeCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Coroutine that performs the actual fade animation with progress callback.
        /// </summary>
        private IEnumerator FadeCoroutineWithProgress(float startAlpha, float endAlpha, Color baseColor, float duration, Action<float> onProgress, Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                fadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                onProgress?.Invoke(progress);
                yield return null;
            }

            // Ensure final alpha is exact
            fadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, endAlpha);
            onProgress?.Invoke(1f);

            currentFadeCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Immediately sets the screen to a specific color and alpha.
        /// Useful for starting a scene already faded out.
        /// </summary>
        /// <param name="color">Color to set (including alpha)</param>
        public void SetImmediate(Color color)
        {
            if (fadeImage == null)
            {
                Debug.LogWarning("ScreenFader: fadeImage is null, cannot set immediate color");
                return;
            }

            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }

            fadeImage.color = color;
        }

        /// <summary>
        /// Returns true if a fade is currently in progress.
        /// </summary>
        public bool IsFading => currentFadeCoroutine != null;
    }
}
