using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace VNWinter.DialogueSystem
{
    public class TypewriterEffect : MonoBehaviour
    {
        [SerializeField] private float defaultCharactersPerSecond = 30f;
        [SerializeField] private float fastForwardCharactersPerSecond = 100f;

        private const float MinCharactersPerSecond = 1f;

        public float DefaultCharactersPerSecond => defaultCharactersPerSecond;
        public float DefaultFastForwardCharactersPerSecond => fastForwardCharactersPerSecond;

        private TMP_Text targetText;
        private string fullText;
        private int visibleCharacters;
        private int totalVisibleCharacters;
        private float characterInterval;
        private bool isTyping;
        private bool isFastForwarding;
        private Action onComplete;
        private Action onHalfway;
        private bool halfwayTriggered;
        private float fastForwardUnlockTime;

        public bool IsTyping => isTyping;
        public bool IsFastForwarding => isFastForwarding;

        public void StartTyping(TMP_Text text, string content, Action onCompleteCallback)
        {
            StartTyping(text, content, defaultCharactersPerSecond, onCompleteCallback);
        }

        public void StartTyping(TMP_Text text, string content, float charsPerSecond, Action onCompleteCallback)
        {
            StartTyping(text, content, charsPerSecond, 0, onCompleteCallback);
        }

        public void StartTyping(TMP_Text text, string content, float charsPerSecond, int initialVisibleCharacters, Action onCompleteCallback)
        {
            StartTyping(text, content, charsPerSecond, initialVisibleCharacters, onCompleteCallback, null);
        }

        public void StartTyping(TMP_Text text, string content, float charsPerSecond, int initialVisibleCharacters, Action onCompleteCallback, Action onHalfwayCallback)
        {
            if (text == null)
            {
                Debug.LogWarning("TypewriterEffect: target text is null; skipping typewriter animation");
                onCompleteCallback?.Invoke();
                return;
            }

            Stop();

            targetText = text;
            fullText = content ?? string.Empty;

            float safeCharsPerSecond = Mathf.Max(charsPerSecond, MinCharactersPerSecond);
            characterInterval = 1f / safeCharsPerSecond;
            onComplete = onCompleteCallback;
            onHalfway = onHalfwayCallback;
            halfwayTriggered = false;
            isFastForwarding = false;
            fastForwardUnlockTime = Time.unscaledTime + 0.1f; // ignore clicks from the same frame/start

            targetText.text = fullText;
            targetText.ForceMeshUpdate();

            totalVisibleCharacters = targetText.textInfo.characterCount;
            if (totalVisibleCharacters <= 0 && fullText.Length > 0)
            {
                // Fallback for cases where textInfo isn't populated yet (disabled canvas, layout pending, etc.)
                totalVisibleCharacters = fullText.Length;
                Debug.LogWarning($"TypewriterEffect: textInfo character count was 0, falling back to string length {totalVisibleCharacters} for '{fullText}'");
            }

            if (totalVisibleCharacters <= 0)
            {
                targetText.maxVisibleCharacters = 0;
                isTyping = false;
                onComplete?.Invoke();
                return;
            }

            visibleCharacters = Mathf.Clamp(initialVisibleCharacters, 0, totalVisibleCharacters);
            targetText.maxVisibleCharacters = visibleCharacters;
            targetText.ForceMeshUpdate();

            if (visibleCharacters >= totalVisibleCharacters)
            {
                isTyping = false;
                onComplete?.Invoke();
                return;
            }

            isTyping = true;
            typingCoroutine = StartCoroutine(TypeTextRoutine());
        }

        public void Complete()
        {
            if (!isTyping || targetText == null) return;

            targetText.maxVisibleCharacters = totalVisibleCharacters;
            isTyping = false;
            isFastForwarding = false;
            onComplete?.Invoke();

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
        }

        public void Stop()
        {
            isTyping = false;
            isFastForwarding = false;
            onComplete = null;

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
        }

        public bool TryFastForward()
        {
            if (!isTyping || targetText == null)
                return false;

            if (isFastForwarding)
                return false;

            if (Time.unscaledTime < fastForwardUnlockTime)
                return false;

            float safeCharsPerSecond = Mathf.Max(fastForwardCharactersPerSecond, MinCharactersPerSecond);
            characterInterval = 1f / safeCharsPerSecond;
            isFastForwarding = true;
            return true;
        }

        private Coroutine typingCoroutine;

        private IEnumerator TypeTextRoutine()
        {
            int halfwayPoint = totalVisibleCharacters / 2;

            while (isTyping && visibleCharacters < totalVisibleCharacters)
            {
                yield return new WaitForSecondsRealtime(characterInterval);
                visibleCharacters++;
                targetText.maxVisibleCharacters = visibleCharacters;

                // Trigger halfway callback
                if (!halfwayTriggered && visibleCharacters >= halfwayPoint && onHalfway != null)
                {
                    halfwayTriggered = true;
                    onHalfway.Invoke();
                }
            }

            if (visibleCharacters >= totalVisibleCharacters)
            {
                isTyping = false;
                isFastForwarding = false;
                onComplete?.Invoke();
            }

            typingCoroutine = null;
        }
    }
}
