using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueSystem
{
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueGraphAsset dialogueGraph;
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private bool startOnAwake = false;

        [Tooltip("Optional UI panel to ignore clicks on (e.g. a phone/overlay panel). Clicks over this panel will not advance dialogue.")]
        [SerializeField] private RectTransform ignoreClickPanel;

        private float advanceDebounceUntil;

        public void SetDialogueGraph(DialogueGraphAsset graph) { dialogueGraph = graph; }
        public void SetDialogueManager(DialogueManager manager) { dialogueManager = manager; }

        private void Start()
        {
            if (startOnAwake && dialogueGraph != null)
            {
                StartDialogue();
            }
        }

        public void StartDialogue()
        {
            if (dialogueManager != null && dialogueGraph != null)
            {
                dialogueManager.StartDialogue(dialogueGraph);
                advanceDebounceUntil = Time.unscaledTime + 0.15f;
            }
        }

        private void Update()
        {
            if (dialogueManager == null) return;
            if (!dialogueManager.IsDialogueActive) return;
            if (Time.unscaledTime < advanceDebounceUntil) return;
            if (Time.timeScale == 0f) return; // Don't advance dialogue while paused

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            bool advancePressed = false;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (!IsPointerOverIgnoredPanel())
                {
                    advancePressed = true;
                }
            }

            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                    advancePressed = true;
            }

            if (advancePressed)
            {
                dialogueManager.AdvanceDialogue();
            }
        }

        /// <summary>
        /// Checks if the mouse pointer is currently over the ignored UI panel or its children.
        /// </summary>
        private bool IsPointerOverIgnoredPanel()
        {
            if (ignoreClickPanel == null) return false;
            if (EventSystem.current == null) return false;
            if (Mouse.current == null) return false;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.transform.IsChildOf(ignoreClickPanel.transform) ||
                    result.gameObject.transform == ignoreClickPanel.transform)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
