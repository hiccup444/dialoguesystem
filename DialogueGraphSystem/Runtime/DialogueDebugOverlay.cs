using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

namespace VNWinter.DialogueSystem
{
    /// <summary>
    /// Debug overlay that displays the current dialogue node's GUID on screen.
    /// Allows writers to note down node IDs while playtesting for easy reference.
    /// Toggle with F2, copy full GUID to clipboard with F3.
    /// </summary>
    public class DialogueDebugOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI instructionsText;

        [Header("Settings")]
        [SerializeField] private bool startVisible = false;

        private bool isVisible = false;
        private string currentFullGuid = "";

        // Input keys (hardcoded for simplicity)
        private const Key TOGGLE_KEY = Key.F2;
        private const Key COPY_KEY = Key.F3;
        private const string TOGGLE_KEY_NAME = "F2";
        private const string COPY_KEY_NAME = "F3";

        private void Awake()
        {
            // Auto-find DialogueManager if not assigned
            if (dialogueManager == null)
            {
                dialogueManager = FindFirstObjectByType<DialogueManager>();
                if (dialogueManager == null)
                {
                    Debug.LogWarning("DialogueDebugOverlay: No DialogueManager found in scene!");
                }
            }

            // Create UI if not assigned
            if (overlayPanel == null)
            {
                CreateDebugUI();
            }

            // Initialize visibility
            isVisible = startVisible;
            UpdateVisibility();
        }

        /// <summary>
        /// Programmatically creates the debug overlay UI if it doesn't exist.
        /// </summary>
        private void CreateDebugUI()
        {
            // Get Canvas component
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }
            if (canvas == null)
            {
                Debug.LogWarning("DialogueDebugOverlay: No Canvas found!");
                return;
            }

            // Find an existing TextMeshProUGUI to copy font settings from
            TextMeshProUGUI existingTMP = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
            TMPro.TMP_FontAsset font = null;
            if (existingTMP != null)
            {
                font = existingTMP.font;
                Debug.Log($"DialogueDebugOverlay: Using font from existing TMP_Text: {font?.name}");
            }
            else
            {
                Debug.LogWarning("DialogueDebugOverlay: No existing TMP_Text found to copy font from. Text may not render correctly.");
            }

            // Create overlay panel
            overlayPanel = new GameObject("DebugOverlay");
            overlayPanel.layer = canvas.gameObject.layer; // Match canvas layer
            overlayPanel.transform.SetParent(canvas.transform, false);

            // Add RectTransform
            RectTransform panelRect = overlayPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);  // Top-left
            panelRect.anchorMax = new Vector2(0, 1);  // Top-left
            panelRect.pivot = new Vector2(0, 1);      // Top-left
            panelRect.anchoredPosition = new Vector2(10, -10); // 10px from top-left corner
            panelRect.sizeDelta = new Vector2(200, 200);

            // Add CanvasGroup for fade control
            overlayPanel.AddComponent<CanvasGroup>();

            // Add background image
            var image = overlayPanel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // Add VerticalLayoutGroup
            var layoutGroup = overlayPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(15, 15, 15, 15);
            layoutGroup.spacing = 5;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            // Create ID text
            GameObject idObj = new GameObject("IDText");
            idObj.layer = canvas.gameObject.layer;
            idObj.transform.SetParent(overlayPanel.transform, false);
            idText = idObj.AddComponent<TextMeshProUGUI>();
            idText.text = "ID: ---";
            idText.fontSize = 24;
            idText.color = Color.white;
            idText.alignment = TextAlignmentOptions.Left;
            if (font != null) idText.font = font;

            // Create Type text
            GameObject typeObj = new GameObject("TypeText");
            typeObj.layer = canvas.gameObject.layer;
            typeObj.transform.SetParent(overlayPanel.transform, false);
            typeText = typeObj.AddComponent<TextMeshProUGUI>();
            typeText.text = "Type: ---";
            typeText.fontSize = 20;
            typeText.color = Color.gray;
            typeText.alignment = TextAlignmentOptions.Left;
            if (font != null) typeText.font = font;

            // Create Instructions text
            GameObject instructionsObj = new GameObject("InstructionsText");
            instructionsObj.layer = canvas.gameObject.layer;
            instructionsObj.transform.SetParent(overlayPanel.transform, false);
            instructionsText = instructionsObj.AddComponent<TextMeshProUGUI>();
            instructionsText.text = $"{TOGGLE_KEY_NAME}: Hide | {COPY_KEY_NAME}: Copy";
            instructionsText.fontSize = 16;
            instructionsText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            instructionsText.alignment = TextAlignmentOptions.Left;
            if (font != null) instructionsText.font = font;

            Debug.Log("DialogueDebugOverlay: Created debug UI programmatically");
        }

        private void OnEnable()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnNodeChanged += OnNodeChanged;
                dialogueManager.OnDialogueEnded += OnDialogueEnded;
            }
        }

        private void OnDisable()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnNodeChanged -= OnNodeChanged;
                dialogueManager.OnDialogueEnded -= OnDialogueEnded;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[TOGGLE_KEY].wasPressedThisFrame)
            {
                ToggleVisibility();
            }

            if (keyboard[COPY_KEY].wasPressedThisFrame && !string.IsNullOrEmpty(currentFullGuid))
            {
                CopyGuidToClipboard();
            }
        }

        private void OnNodeChanged(DialogueGraph.BaseNodeData node)
        {
            if (node == null) return;

            // Store full GUID
            currentFullGuid = dialogueManager.CurrentNodeGuid;

            // Update display
            UpdateDisplay();
        }

        private void OnDialogueEnded()
        {
            // Clear display when dialogue ends
            currentFullGuid = "";
            if (idText != null) idText.text = "ID: ---";
            if (typeText != null) typeText.text = "Type: ---";
        }

        private void UpdateDisplay()
        {
            if (string.IsNullOrEmpty(currentFullGuid))
            {
                if (idText != null) idText.text = "ID: ---";
                if (typeText != null) typeText.text = "Type: ---";
                return;
            }

            // Show shortened GUID (first 8 characters)
            string shortGuid = currentFullGuid.Length >= 8
                ? currentFullGuid.Substring(0, 8)
                : currentFullGuid;

            if (idText != null)
            {
                idText.text = $"ID: {shortGuid}";
            }

            if (typeText != null)
            {
                string nodeType = dialogueManager.CurrentNodeType ?? "Unknown";
                typeText.text = $"Type: {nodeType}";
            }
        }

        private void ToggleVisibility()
        {
            isVisible = !isVisible;
            UpdateVisibility();

            // Update instructions text when toggling
            if (isVisible && instructionsText != null)
            {
                instructionsText.text = $"{TOGGLE_KEY_NAME}: Hide | {COPY_KEY_NAME}: Copy";
            }
        }

        private void UpdateVisibility()
        {
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(isVisible);
            }

            // Update display when becoming visible
            if (isVisible)
            {
                UpdateDisplay();
            }
        }

        private void CopyGuidToClipboard()
        {
            if (string.IsNullOrEmpty(currentFullGuid))
            {
                Debug.LogWarning("DialogueDebugOverlay: No GUID to copy");
                return;
            }

            GUIUtility.systemCopyBuffer = currentFullGuid;
            Debug.Log($"DialogueDebugOverlay: Copied GUID to clipboard: {currentFullGuid}");

            // Optional: Flash the text to indicate copy success
            if (idText != null)
            {
                StartCoroutine(FlashCopyFeedback());
            }
        }

        private System.Collections.IEnumerator FlashCopyFeedback()
        {
            Color originalColor = idText.color;
            idText.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            idText.color = originalColor;
        }

        #region Editor Helpers

        #if UNITY_EDITOR
        [ContextMenu("Find UI References")]
        private void FindUIReferences()
        {
            if (overlayPanel == null)
            {
                // Try to find overlay panel by name
                var panel = transform.Find("DebugOverlay");
                if (panel != null)
                {
                    overlayPanel = panel.gameObject;
                    Debug.Log("DialogueDebugOverlay: Found DebugOverlay panel");
                }
            }

            if (overlayPanel != null)
            {
                // Try to find text components
                var texts = overlayPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    if (text.name.Contains("ID") && idText == null)
                    {
                        idText = text;
                        Debug.Log($"DialogueDebugOverlay: Found ID text: {text.name}");
                    }
                    else if (text.name.Contains("Type") && typeText == null)
                    {
                        typeText = text;
                        Debug.Log($"DialogueDebugOverlay: Found Type text: {text.name}");
                    }
                    else if (text.name.Contains("Instructions") && instructionsText == null)
                    {
                        instructionsText = text;
                        Debug.Log($"DialogueDebugOverlay: Found Instructions text: {text.name}");
                    }
                }
            }
        }
        #endif

        #endregion
    }
}
