using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueSystem
{
    public class DialogueDebugManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private DialogueUI dialogueUI;

        [Header("Debug Graphs")]
        [SerializeField] private List<DialogueGraphAsset> debugGraphs = new List<DialogueGraphAsset>();

        [Header("Settings")]
        [SerializeField] private Key triggerKey = Key.F1;
        [SerializeField] private Key skipDialogueKey = Key.F4;
        [SerializeField] private bool useNumberKeys = true;
        [SerializeField] private bool handleDialogueInput = true;

        private void Update()
        {
            if (!Application.isPlaying) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // Handle dialogue advancement input
            if (handleDialogueInput && dialogueManager != null && dialogueManager.IsDialogueActive)
            {
                bool advancePressed = false;

                // Check mouse click, but ignore if clicking on UI elements (buttons, phone, pause menu, etc.)
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    // Only advance dialogue if NOT clicking on UI elements
                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                    {
                        advancePressed = true;
                    }
                }

                // Keyboard input is always allowed
                if (keyboard != null)
                {
                    if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                        advancePressed = true;
                }

                if (advancePressed)
                {
                    dialogueManager.AdvanceDialogue();
                    return;
                }
            }

            if (keyboard == null) return;

            // Skip current dialogue graph with assigned key
            if (dialogueManager != null && dialogueManager.IsDialogueActive)
            {
                if (keyboard[skipDialogueKey].wasPressedThisFrame)
                {
                    Debug.Log($"DialogueDebugManager: Skipping current dialogue graph (key: {skipDialogueKey})");
                    dialogueManager.EndDialogue();
                    return;
                }
            }

            // Check number keys 1-9 for quick graph selection (only when dialogue not active)
            if (useNumberKeys && (dialogueManager == null || !dialogueManager.IsDialogueActive))
            {
                for (int i = 0; i < Mathf.Min(debugGraphs.Count, 9); i++)
                {
                    Key numKey = Key.Digit1 + i;
                    if (keyboard[numKey].wasPressedThisFrame)
                    {
                        TriggerGraph(i);
                        return;
                    }
                }
            }

            // Trigger key triggers first graph
            if (keyboard[triggerKey].wasPressedThisFrame && debugGraphs.Count > 0)
            {
                TriggerGraph(0);
            }
        }

        public void TriggerGraph(int index)
        {
            if (index < 0 || index >= debugGraphs.Count)
            {
                Debug.LogWarning($"DialogueDebugManager: Invalid graph index {index}");
                return;
            }

            var graph = debugGraphs[index];
            if (graph == null)
            {
                Debug.LogWarning($"DialogueDebugManager: Graph at index {index} is null");
                return;
            }

            if (dialogueManager == null)
            {
                Debug.LogError("DialogueDebugManager: DialogueManager reference is not set");
                return;
            }

            if (dialogueManager.IsDialogueActive)
            {
                Debug.Log("DialogueDebugManager: Dialogue already active, ignoring trigger");
                return;
            }

            Debug.Log($"DialogueDebugManager: Triggering graph '{graph.name}'");

            // Start dialogue and fade in
            dialogueManager.StartDialogue(graph);

            if (dialogueUI != null)
            {
                dialogueUI.FadeIn();
            }
        }

        public void TriggerGraph(DialogueGraphAsset graph)
        {
            if (graph == null)
            {
                Debug.LogWarning("DialogueDebugManager: Cannot trigger null graph");
                return;
            }

            if (dialogueManager == null)
            {
                Debug.LogError("DialogueDebugManager: DialogueManager reference is not set");
                return;
            }

            if (dialogueManager.IsDialogueActive)
            {
                Debug.Log("DialogueDebugManager: Dialogue already active, ignoring trigger");
                return;
            }

            Debug.Log($"DialogueDebugManager: Triggering graph '{graph.name}'");

            dialogueManager.StartDialogue(graph);

            if (dialogueUI != null)
            {
                dialogueUI.FadeIn();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Trigger First Graph")]
        private void TriggerFirstGraph()
        {
            if (Application.isPlaying && debugGraphs.Count > 0)
            {
                TriggerGraph(0);
            }
        }
#endif
    }
}
