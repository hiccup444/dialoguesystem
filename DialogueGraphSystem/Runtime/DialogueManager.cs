using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueSystem
{
    // TODO: Refactor state handling to use formal State Pattern
    // Current switch-based approach works but could be cleaner with IDialogueState interface
    // See: https://refactoring.guru/design-patterns/state

    /// <summary>
    /// Tracks the current phase of dialogue playback.
    /// </summary>
    public enum DialogueState
    {
        /// <summary>No dialogue is active.</summary>
        Inactive,
        /// <summary>Determining how to handle the current node.</summary>
        ProcessingNode,
        /// <summary>Typewriter effect is animating text.</summary>
        DisplayingText,
        /// <summary>Text is complete, waiting for player to click/continue.</summary>
        WaitingForInput,
        /// <summary>Choice buttons are animating in.</summary>
        FadingInChoices,
        /// <summary>Choice buttons visible, waiting for player selection.</summary>
        WaitingForChoice,
        /// <summary>Dialogue is cleaning up and closing.</summary>
        Ending
    }

    /// <summary>
    /// Core runtime manager for dialogue playback. Traverses DialogueGraphAsset nodes
    /// and coordinates with DialogueUI for display. Attach to a GameObject in your scene.
    ///
    /// Usage:
    ///   dialogueManager.StartDialogue(myGraphAsset);
    ///   // Call AdvanceDialogue() on click/input to progress
    ///   // Subscribe to events for custom behavior
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private Transform characterSpawnContainer;

        [Header("Audio")]
        [SerializeField] private AudioClip choiceSelectClip;

        private AudioSource audioSource;
        private DialogueGraphAsset currentGraph;
        private BaseNodeData currentNode;
        private DialogueState state = DialogueState.Inactive;
        private bool isAutoTriggered = false;

        private int conversationPointsTotal;

        public event Action<int> OnConversationPointsChanged;

        public int ConversationPointsTotal => conversationPointsTotal;

        // Track spawned characters
        private GameObject currentSpawnedCharacter;
        private bool currentCharacterPersists;

        #region Events

        /// <summary>Fired when a dialogue graph begins playing.</summary>
        public event Action<DialogueGraphAsset> OnDialogueStarted;

        /// <summary>Fired when dialogue ends (reached End node or stopped).</summary>
        public event Action OnDialogueEnded;

        /// <summary>Fired when entering a dialogue node. Use for audio, animations, etc.</summary>
        public event Action<DialogueNodeData> OnDialogueNodeEntered;

        /// <summary>Fired when entering a choice node, before choices are displayed.</summary>
        public event Action<ChoiceNodeData> OnChoiceNodeEntered;

        /// <summary>Fired when player selects a choice. Contains the selected ChoiceData.</summary>
        public event Action<ChoiceData> OnChoiceSelected;

        /// <summary>Fired whenever the current node changes. Useful for debugging/logging.</summary>
        public event Action<BaseNodeData> OnNodeChanged;

        /// <summary>Fired when an EventNode is processed. Subscribe to handle game events.</summary>
        public event Action<EventNodeData> OnDialogueEvent;

        /// <summary>Fired when an AudioNode is processed. Subscribe to play audio in your own audio system.</summary>
        public event Action<AudioNodeData> OnAudioNodeExecuted;

        /// <summary>Fired when a StopSoundNode is processed. Subscribe to stop audio in your own audio system.</summary>
        public event Action<StopSoundNodeData> OnStopSoundNodeExecuted;

        /// <summary>Fired when a SaveAudioPlaybackNode is processed. Subscribe to save audio state.</summary>
        public event Action OnSaveAudioPlaybackExecuted;

        /// <summary>Fired when a RoomTransitionNode is processed. Subscribe to load the target scene/room.</summary>
        public event Action<RoomTransitionNodeData> OnRoomTransitionExecuted;

        /// <summary>
        /// Optional delegate polled while waiting for a room transition to complete.
        /// Return true while transitioning, false when done.
        /// Example: dialogueManager.IsRoomTransitioning = () => RoomManager.Instance.IsTransitioning;
        /// </summary>
        public Func<bool> IsRoomTransitioning;

        /// <summary>
        /// Optional delegate to resolve an emotion swap sprite for a speaker.
        /// Called with (speakerName, emotionTypeName) where emotionTypeName is "neutral", "positive", or "negative".
        /// Return the Sprite to use, or null to skip the swap.
        /// Example: dialogueManager.GetEmotionSprite = (speaker, emotion) => characterDb.GetSprite(speaker, emotion);
        /// </summary>
        public Func<string, string, Sprite> GetEmotionSprite;

        #endregion

        /// <summary>True if dialogue is currently active (any state except Inactive).</summary>
        public bool IsDialogueActive => state != DialogueState.Inactive;

        /// <summary>Current state of the dialogue system.</summary>
        public DialogueState CurrentState => state;

        /// <summary>GUID of the current dialogue node (null if no dialogue active).</summary>
        public string CurrentNodeGuid { get; private set; }

        /// <summary>Type of the current dialogue node (e.g., "Dialogue", "Choice", "End").</summary>
        public string CurrentNodeType { get; private set; }

        private void Awake()
        {
            // Get or add AudioSource for dialogue sounds
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        #region Public API

        /// <summary>
        /// Begins playing a dialogue graph from its Start node.
        /// </summary>
        /// <param name="graph">The dialogue graph asset to play.</param>
        /// <param name="isAutoTriggered">True if dialogue was auto-triggered by room entry, false if manually started.</param>
        public void StartDialogue(DialogueGraphAsset graph, bool isAutoTriggered = false)
        {
            if (graph == null)
            {
                Debug.LogError("DialogueManager: Cannot start dialogue with null graph");
                return;
            }

            if (graph.StartNode == null)
            {
                Debug.LogError($"DialogueManager: Graph '{graph.name}' has no start node");
                return;
            }

            StopAllCoroutines();
            currentGraph = graph;
            state = DialogueState.ProcessingNode;
            this.isAutoTriggered = isAutoTriggered;

            ResetConversationPoints();

            dialogueUI.ResetDialogueHistory();
            dialogueUI.Show();
            OnDialogueStarted?.Invoke(graph);

            MoveToNextNode(graph.StartNode.guid, "Start");
        }

        /// <summary>
        /// Advances dialogue when player clicks/presses continue.
        /// If text is still typing, completes it instantly.
        /// If waiting for input, moves to the next node.
        /// </summary>
        public void AdvanceDialogue()
        {
            switch (state)
            {
                case DialogueState.DisplayingText:
                    if (dialogueUI.IsTyping())
                    {
                        // First click while typing: fast-forward; second click: complete instantly
                        if (dialogueUI.TryFastForwardTypewriter())
                        {
                            return;
                        }
                        return;
                    }
                    break;

                case DialogueState.WaitingForInput:
                    // Move to next node
                    dialogueUI.HideContinueIndicator();
                    MoveToNextNode(currentNode.guid, "Output");
                    break;
            }
        }

        /// <summary>
        /// Selects a choice by index when at a choice node.
        /// Called by UI button handlers.
        /// </summary>
        /// <param name="index">Zero-based index of the choice to select.</param>
        public void SelectChoice(int index)
        {
            if (state != DialogueState.WaitingForChoice) return;
            if (currentNode is not ChoiceNodeData choiceNode) return;
            if (index < 0 || index >= choiceNode.choices.Count) return;

            // Play choice selection sound
            if (choiceSelectClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(choiceSelectClip);
            }

            var selectedChoice = choiceNode.choices[index];
            OnChoiceSelected?.Invoke(selectedChoice);

            dialogueUI.HideChoices();

            AddConversationPoints(selectedChoice.conversationPoints);

            // Use the choice's GUID to find the correct output connection
            MoveToNextNode(currentNode.guid, selectedChoice.choiceGuid);
        }

        /// <summary>
        /// Forcefully stops dialogue playback. Use for skip functionality or interruptions.
        /// </summary>
        public void StopDialogue()
        {
            if (state == DialogueState.Inactive) return;

            state = DialogueState.Inactive;
            currentGraph = null;
            currentNode = null;

            dialogueUI.Hide();
            OnDialogueEnded?.Invoke();
        }

        #endregion

        #region Node Processing

        /// <summary>
        /// Routes the current node to its appropriate handler based on type.
        /// This is the main dispatch method for the state machine.
        /// </summary>
        private void ProcessCurrentNode()
        {
            if (currentNode == null)
            {
                EndDialogue();
                return;
            }

            // Update current node info for debug tools
            CurrentNodeGuid = currentNode.guid;
            CurrentNodeType = currentNode.nodeType;

            OnNodeChanged?.Invoke(currentNode);

            switch (currentNode)
            {
                case StartNodeData:
                    // Start node just passes through to its connected node
                    MoveToNextNode(currentNode.guid, "Start");
                    break;

                case DialogueNodeData dialogueNode:
                    ProcessDialogueNode(dialogueNode);
                    break;

                case ChoiceNodeData choiceNode:
                    ProcessChoiceNode(choiceNode);
                    break;

                case EndNodeData:
                    EndDialogue();
                    break;

                case RandomNodeData randomNode:
                    ProcessRandomNode(randomNode);
                    break;

                case VariableNodeData variableNode:
                    ProcessVariableNode(variableNode);
                    break;

                case ConditionalNodeData conditionalNode:
                    ProcessConditionalNode(conditionalNode);
                    break;

                case EventNodeData eventNode:
                    ProcessEventNode(eventNode);
                    break;

                case RoomTransitionNodeData transitionNode:
                    ProcessRoomTransitionNode(transitionNode);
                    break;

                case FadeNodeData fadeNode:
                    ProcessFadeNode(fadeNode);
                    break;

                case GraphTransitionNodeData transitionNode:
                    ProcessGraphTransitionNode(transitionNode);
                    break;

                case PreventAutoTriggerNodeData preventAutoTriggerNode:
                    ProcessPreventAutoTriggerNode(preventAutoTriggerNode);
                    break;

                case SceneImageNodeData sceneImageNode:
                    ProcessSceneImageNode(sceneImageNode);
                    break;

                case AudioNodeData audioNode:
                    ProcessAudioNode(audioNode);
                    break;

                case StopSoundNodeData stopSoundNode:
                    ProcessStopSoundNode(stopSoundNode);
                    break;

                case SaveAudioPlaybackNodeData saveAudioPlaybackNode:
                    ProcessSaveAudioPlaybackNode(saveAudioPlaybackNode);
                    break;

                case NotLastCharacterNodeData notLastCharacterNode:
                    // This node is just a marker - pass through to next node
                    ProcessPassThroughNode(notLastCharacterNode);
                    break;

                case StartFrostingNodeData startFrostingNode:
                    ProcessStartFrostingNode(startFrostingNode);
                    break;

                default:
                    Debug.LogWarning($"DialogueManager: Unknown node type: {currentNode.nodeType}");
                    EndDialogue();
                    break;
            }
        }

        /// <summary>Displays dialogue text with typewriter effect.</summary>
        private void ProcessDialogueNode(DialogueNodeData dialogueNode)
        {
            state = DialogueState.DisplayingText;
            OnDialogueNodeEntered?.Invoke(dialogueNode);

            // Handle character spawning
            HandleCharacterSpawn(dialogueNode);

            // Resolve emotion swap sprite via delegate (if assigned)
            Sprite emotionSwapSprite = null;
            if (dialogueNode.emotionSwapEnabled && GetEmotionSprite != null)
            {
                emotionSwapSprite = GetEmotionSprite(dialogueNode.speakerName, dialogueNode.emotionType.ToString().ToLower());
            }

            // Process variable replacements in dialogue text
            string processedText = ReplaceDialogueVariables(dialogueNode.dialogueText);

            dialogueUI.ShowDialogue(
                dialogueNode.speakerName,
                processedText,
                dialogueNode.portrait,
                dialogueNode.showPortraitPanel,
                dialogueNode.showSpeakerName,
                dialogueNode.isThought,
                dialogueNode.useEndingPortraitFront,
                dialogueNode.endingPortraitFrontIndex,
                dialogueNode.useCustomPositioning,
                dialogueNode.portraitPosition,
                dialogueNode.portraitScale,
                dialogueNode.portraitAnchor,
                dialogueNode.portraitSize,
                dialogueNode.flipPortrait,
                dialogueNode.emotionSwapEnabled,
                dialogueNode.emotionSwapTiming,
                emotionSwapSprite,
                dialogueNode.swayEnabled,
                dialogueNode.swayPattern,
                dialogueNode.swaySpeed,
                dialogueNode.swayIntensity,
                dialogueNode.sway2Enabled,
                dialogueNode.sway2Pattern,
                dialogueNode.sway2Speed,
                dialogueNode.sway2Intensity,
                dialogueNode.rotationEnabled,
                dialogueNode.rotationAxis,
                dialogueNode.rotationMinAngle,
                dialogueNode.rotationMaxAngle,
                dialogueNode.rotationSpeed,
                dialogueNode.scaleEnabled,
                dialogueNode.scaleMin,
                dialogueNode.scaleMax,
                dialogueNode.scaleSpeed,
                dialogueNode.speakingScaleEnabled,
                OnTypewriterComplete
            );
        }

        /// <summary>
        /// Replaces variable placeholders in dialogue text with their values.
        /// Supports: {last_character}, {current_character}, and any string variable as {variable_name}
        /// </summary>
        private string ReplaceDialogueVariables(string text)
        {
            if (string.IsNullOrEmpty(text) || GlobalVariables.Instance == null)
                return text;

            // Replace {last_character} with the last character talked to
            if (text.Contains("{last_character}"))
            {
                string lastChar = GlobalVariables.Instance.GetString("last_character");
                text = text.Replace("{last_character}", lastChar);
            }

            // Replace {current_character} with the current character being talked to
            if (text.Contains("{current_character}"))
            {
                string currentChar = GlobalVariables.Instance.GetString("current_character");
                text = text.Replace("{current_character}", currentChar);
            }

            // Generic replacement for any {variable_name} pattern with string variables
            int startIndex = 0;
            while ((startIndex = text.IndexOf('{', startIndex)) != -1)
            {
                int endIndex = text.IndexOf('}', startIndex);
                if (endIndex == -1) break;

                string varName = text.Substring(startIndex + 1, endIndex - startIndex - 1);
                string value = GlobalVariables.Instance.GetString(varName);

                // Only replace if we found a non-empty value (to avoid breaking intended curly braces)
                if (!string.IsNullOrEmpty(value))
                {
                    text = text.Substring(0, startIndex) + value + text.Substring(endIndex + 1);
                    // Don't advance startIndex since we replaced the text
                }
                else
                {
                    // Move past this placeholder to avoid infinite loop
                    startIndex = endIndex + 1;
                }
            }

            return text;
        }

        /// <summary>
        /// Called when typewriter finishes. Either shows continue indicator
        /// or auto-advances to choices if next node is a ChoiceNode.
        /// </summary>
        private void OnTypewriterComplete()
        {
            if (state == DialogueState.DisplayingText)
            {
                var nextNode = GetNextNodeFromOutput(currentNode.guid, "Output");

                // Check if current node requires interaction (for DialogueNodes)
                bool requiresClick = true;
                if (currentNode is DialogueNodeData dialogueData)
                {
                    requiresClick = dialogueData.requireInteraction;
                }

                // If requireInteraction is false, auto-advance without waiting for click
                if (!requiresClick)
                {
                    MoveToNextNode(currentNode.guid, "Output");
                }
                // Auto-advance to choices without requiring click
                else if (nextNode is ChoiceNodeData)
                {
                    MoveToNextNode(currentNode.guid, "Output");
                }
                else
                {
                    // requiresClick is true - wait for player input before continuing
                    state = DialogueState.WaitingForInput;
                    dialogueUI.ShowContinueIndicator();
                }
            }
        }

        /// <summary>Checks if the next connected node is a ChoiceNode.</summary>
        private bool IsNextNodeChoice()
        {
            var connections = currentGraph.GetConnectionsFromNode(currentNode.guid);
            foreach (var conn in connections)
            {
                if (conn.outputPortName == "Output")
                {
                    var nextNode = currentGraph.GetNodeByGuid(conn.inputNodeGuid);
                    return nextNode is ChoiceNodeData;
                }
            }
            return false;
        }

        /// <summary>Shows choice buttons and waits for player selection.</summary>
        private void ProcessChoiceNode(ChoiceNodeData choiceNode)
        {
            state = DialogueState.FadingInChoices;
            OnChoiceNodeEntered?.Invoke(choiceNode);

            dialogueUI.HideContinueIndicator();
            dialogueUI.ShowChoices(
                choiceNode.choices,
                SelectChoice,
                OnChoicesFadedIn
            );
        }

        private void AddConversationPoints(int points)
        {
            if (points == 0)
                return;

            conversationPointsTotal += points;
            UpdateConversationPointsDisplay();

            // Persist points to GlobalVariables
            if (GlobalVariables.Instance != null)
            {
                // Add to cumulative total across all conversations
                GlobalVariables.Instance.AddInt("total_conversation_points", points);

                // Add to per-character points using current_character
                string currentCharacter = GlobalVariables.Instance.GetString("current_character");
                if (!string.IsNullOrEmpty(currentCharacter))
                {
                    // Use character name (lowercase, spaces replaced with underscores) as key
                    string characterKey = currentCharacter.ToLower().Replace(" ", "_") + "_points";
                    GlobalVariables.Instance.AddInt(characterKey, points);
                }
            }
        }

        private void ResetConversationPoints()
        {
            conversationPointsTotal = 0;
            UpdateConversationPointsDisplay();
        }

        private void UpdateConversationPointsDisplay()
        {
            dialogueUI?.SetConversationPoints(conversationPointsTotal);
            OnConversationPointsChanged?.Invoke(conversationPointsTotal);
        }

        /// <summary>
        /// Modifies a GlobalVariables value and continues to next node.
        /// Variable nodes are pass-through (no player interaction).
        /// </summary>
        private void ProcessVariableNode(VariableNodeData variableNode)
        {
            if (GlobalVariables.Instance == null)
            {
                Debug.LogWarning("DialogueManager: GlobalVariables instance not found. Variable operation skipped.");
            }
            else
            {
                switch (variableNode.variableType)
                {
                    case VariableType.Int:
                        switch (variableNode.intOperation)
                        {
                            case IntOperation.Set:
                                GlobalVariables.Instance.SetInt(variableNode.variableName, variableNode.intValue);
                                break;
                            case IntOperation.Add:
                                GlobalVariables.Instance.AddInt(variableNode.variableName, variableNode.intValue);
                                break;
                            case IntOperation.Subtract:
                                GlobalVariables.Instance.SubtractInt(variableNode.variableName, variableNode.intValue);
                                break;
                        }
                        break;

                    case VariableType.Bool:
                        switch (variableNode.boolOperation)
                        {
                            case BoolOperation.Set:
                                GlobalVariables.Instance.SetBool(variableNode.variableName, variableNode.boolValue);
                                break;
                            case BoolOperation.Toggle:
                                GlobalVariables.Instance.ToggleBool(variableNode.variableName);
                                break;
                        }
                        break;
                }
            }

            MoveToNextNode(currentNode.guid, "Output");
        }

        /// <summary>
        /// Evaluates a condition and routes to True or False output port.
        /// Conditional nodes are pass-through (no player interaction).
        /// </summary>
        private void ProcessConditionalNode(ConditionalNodeData conditionalNode)
        {
            bool conditionMet = false;

            if (GlobalVariables.Instance == null)
            {
                Debug.LogWarning("DialogueManager: GlobalVariables instance not found. Condition defaults to false.");
            }
            else
            {
                switch (conditionalNode.variableType)
                {
                    case VariableType.Int:
                        int intValue = GlobalVariables.Instance.GetInt(conditionalNode.variableName);
                        conditionMet = conditionalNode.comparison switch
                        {
                            ComparisonOperator.Equal => intValue == conditionalNode.compareValue,
                            ComparisonOperator.NotEqual => intValue != conditionalNode.compareValue,
                            ComparisonOperator.GreaterThan => intValue > conditionalNode.compareValue,
                            ComparisonOperator.LessThan => intValue < conditionalNode.compareValue,
                            ComparisonOperator.GreaterOrEqual => intValue >= conditionalNode.compareValue,
                            ComparisonOperator.LessOrEqual => intValue <= conditionalNode.compareValue,
                            _ => false
                        };
                        break;

                    case VariableType.Bool:
                        bool boolValue = GlobalVariables.Instance.GetBool(conditionalNode.variableName);
                        conditionMet = boolValue == conditionalNode.expectedBoolValue;
                        break;
                }
            }

            // Route based on condition result
            string outputPort = conditionMet ? conditionalNode.truePortGuid : conditionalNode.falsePortGuid;
            MoveToNextNode(currentNode.guid, outputPort);
        }

        /// <summary>
        /// Randomly selects one of up to 4 output paths and continues dialogue.
        /// Random nodes are pass-through (no player interaction).
        /// </summary>
        private void ProcessRandomNode(RandomNodeData randomNode)
        {
            // Collect all valid output port GUIDs (non-null and non-empty)
            var validOutputs = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(randomNode.output1PortGuid))
                validOutputs.Add(randomNode.output1PortGuid);
            if (!string.IsNullOrEmpty(randomNode.output2PortGuid))
                validOutputs.Add(randomNode.output2PortGuid);
            if (!string.IsNullOrEmpty(randomNode.output3PortGuid))
                validOutputs.Add(randomNode.output3PortGuid);
            if (!string.IsNullOrEmpty(randomNode.output4PortGuid))
                validOutputs.Add(randomNode.output4PortGuid);

            if (validOutputs.Count == 0)
            {
                Debug.LogWarning("DialogueManager: Random node has no connected outputs. Ending dialogue.");
                EndDialogue();
                return;
            }

            // Randomly select one of the valid outputs
            int randomIndex = UnityEngine.Random.Range(0, validOutputs.Count);
            string selectedOutput = validOutputs[randomIndex];

            MoveToNextNode(currentNode.guid, selectedOutput);
        }

        /// <summary>
        /// Fires an event for external handlers (background changes, music, etc).
        /// Can optionally wait for transitionDuration before continuing.
        /// </summary>
        private void ProcessEventNode(EventNodeData eventNode)
        {
            OnDialogueEvent?.Invoke(eventNode);

            if (eventNode.waitForCompletion && eventNode.transitionDuration > 0)
            {
                StartCoroutine(WaitAndContinue(eventNode.transitionDuration));
            }
            else
            {
                MoveToNextNode(currentNode.guid, "Output");
            }
        }

        /// <summary>
        /// Processes Room Transition node. Fires OnRoomTransitionExecuted — subscribe to load the scene/room.
        /// Assign IsRoomTransitioning delegate to enable wait-for-completion polling.
        /// </summary>
        private void ProcessRoomTransitionNode(RoomTransitionNodeData transitionNode)
        {
            if (string.IsNullOrEmpty(transitionNode.targetSceneName))
            {
                Debug.LogWarning("DialogueManager: RoomTransition node has no target scene name.");
                MoveToNextNode(currentNode.guid, "Output");
                return;
            }

            dialogueUI.HideDialogueElements();

            OnRoomTransitionExecuted?.Invoke(transitionNode);

            if (transitionNode.waitForTransitionCompletion || transitionNode.postTransitionDelay > 0f)
            {
                StartCoroutine(WaitForRoomTransition(transitionNode));
            }
            else
            {
                MoveToNextNode(currentNode.guid, "Output");
            }
        }

        private IEnumerator WaitForRoomTransition(RoomTransitionNodeData node)
        {
            if (node.waitForTransitionCompletion && IsRoomTransitioning != null)
            {
                while (IsRoomTransitioning())
                {
                    yield return null;
                }
            }

            if (node.postTransitionDelay > 0f)
            {
                yield return new WaitForSeconds(node.postTransitionDelay);
            }

            MoveToNextNode(currentNode.guid, "Output");
        }

        /// <summary>Processes fade screen effects.</summary>
        private void ProcessFadeNode(FadeNodeData fadeNode)
        {
            // Hide dialogue box and speaker name so they don't show through the fade
            dialogueUI.HideDialogueElements();

            System.Action onComplete = () =>
            {
                if (fadeNode.waitForCompletion)
                {
                    MoveToNextNode(currentNode.guid, "Output");
                }
            };

            switch (fadeNode.fadeType)
            {
                case FadeNodeData.FadeType.FadeOut:
                    ScreenFader.Instance.FadeOut(fadeNode.fadeColor, fadeNode.fadeDuration, onComplete);
                    break;

                case FadeNodeData.FadeType.FadeIn:
                    ScreenFader.Instance.FadeIn(fadeNode.fadeDuration, onComplete);
                    break;

                case FadeNodeData.FadeType.FadeToColor:
                    ScreenFader.Instance.FadeOut(fadeNode.fadeColor, fadeNode.fadeDuration, onComplete);
                    break;

                case FadeNodeData.FadeType.SolidColor:
                    // Set color immediately and hold for duration
                    ScreenFader.Instance.SetImmediate(fadeNode.fadeColor);
                    StartCoroutine(WaitAndCallback(fadeNode.fadeDuration, onComplete));
                    break;
            }

            // If not waiting for completion, advance immediately
            if (!fadeNode.waitForCompletion)
            {
                MoveToNextNode(currentNode.guid, "Output");
            }
        }

        private void ProcessGraphTransitionNode(GraphTransitionNodeData transitionNode)
        {
            if (transitionNode.targetGraph == null)
            {
                Debug.LogWarning("DialogueManager: GraphTransition node missing target graph.");
                EndDialogue();
                return;
            }

            var nextGraph = transitionNode.targetGraph;

            EndDialogue();
            StartDialogue(nextGraph);
        }

        /// <summary>
        /// Processes PreventAutoTrigger node. If dialogue was auto-triggered, ends it here.
        /// If manually started, continues normally through the output port.
        /// </summary>
        private void ProcessPreventAutoTriggerNode(PreventAutoTriggerNodeData preventAutoTriggerNode)
        {
            if (isAutoTriggered)
            {
                EndDialogue();
            }
            else
            {
                MoveToNextNode(currentNode.guid, "Output");
            }
        }

        /// <summary>
        /// Processes SceneImage node. Finds a GameObject by name and replaces its Image component sprite.
        /// </summary>
        private void ProcessSceneImageNode(SceneImageNodeData sceneImageNode)
        {
            if (string.IsNullOrEmpty(sceneImageNode.targetObjectName))
            {
                Debug.LogWarning("DialogueManager: SceneImage node has no target object name specified");
                MoveToNextNode(currentNode.guid, "Output");
                return;
            }

            // Find the target GameObject by name (including disabled objects)
            GameObject targetObject = FindObjectByNameIncludingDisabled(sceneImageNode.targetObjectName);
            if (targetObject == null)
            {
                Debug.LogWarning($"DialogueManager: SceneImage node could not find object named '{sceneImageNode.targetObjectName}'");
                MoveToNextNode(currentNode.guid, "Output");
                return;
            }

            // Get the Image component
            Image imageComponent = targetObject.GetComponent<Image>();
            if (imageComponent == null)
            {
                Debug.LogWarning($"DialogueManager: Object '{sceneImageNode.targetObjectName}' does not have an Image component");
                MoveToNextNode(currentNode.guid, "Output");
                return;
            }

            // Replace the sprite
            if (sceneImageNode.imageSprite != null)
            {
                imageComponent.sprite = sceneImageNode.imageSprite;
            }

            // Apply opacity
            Color color = imageComponent.color;
            color.a = sceneImageNode.opacity;
            imageComponent.color = color;

            MoveToNextNode(currentNode.guid, "Output");
        }

        /// <summary>
        /// Processes Audio node. Fires OnAudioNodeExecuted — subscribe to handle playback in your audio system.
        /// </summary>
        private void ProcessAudioNode(AudioNodeData audioNode)
        {
            OnAudioNodeExecuted?.Invoke(audioNode);

            if (audioNode.waitForCompletion && audioNode.fadeDuration > 0)
            {
                StartCoroutine(WaitAndContinueAudio(audioNode.fadeDuration));
            }
            else
            {
                MoveToNextNode(currentNode.guid, "Output");
            }
        }

        private IEnumerator WaitAndContinueAudio(float duration)
        {
            yield return new WaitForSeconds(duration);
            MoveToNextNode(currentNode.guid, "Output");
        }

        /// <summary>
        /// Processes Stop Sound node. Fires OnStopSoundNodeExecuted — subscribe to stop audio in your audio system.
        /// </summary>
        private void ProcessStopSoundNode(StopSoundNodeData stopSoundNode)
        {
            OnStopSoundNodeExecuted?.Invoke(stopSoundNode);

            if (stopSoundNode.waitForCompletion && stopSoundNode.fadeOutDuration > 0)
            {
                StartCoroutine(WaitAndContinueAudio(stopSoundNode.fadeOutDuration));
            }
            else
            {
                MoveToNextNode(currentNode.guid, "Output");
            }
        }

        /// <summary>
        /// Processes Save Audio Playback node. Fires OnSaveAudioPlaybackExecuted — subscribe to persist audio state.
        /// </summary>
        private void ProcessSaveAudioPlaybackNode(SaveAudioPlaybackNodeData saveAudioPlaybackNode)
        {
            OnSaveAudioPlaybackExecuted?.Invoke();
            MoveToNextNode(currentNode.guid, "Output");
        }

        /// <summary>
        /// Processes a pass-through node that has no behavior other than advancing to the next node.
        /// Used for marker nodes like NotLastCharacter.
        /// </summary>
        private void ProcessPassThroughNode(BaseNodeData node)
        {
            MoveToNextNode(node.guid, "Output");
        }

        /// <summary>
        /// Processes Start Frosting node. Fires OnDialogueEvent with a Custom "StartFrosting" key and ends dialogue.
        /// Subscribe to OnDialogueEvent and check eventKey == "StartFrosting" to handle in your game.
        /// </summary>
        private void ProcessStartFrostingNode(StartFrostingNodeData startFrostingNode)
        {
            var syntheticEvent = new EventNodeData { eventType = DialogueEventType.Custom, eventKey = "StartFrosting" };
            OnDialogueEvent?.Invoke(syntheticEvent);
            EndDialogue();
        }

        /// <summary>
        /// Finds a GameObject by name, including disabled objects.
        /// </summary>
        private GameObject FindObjectByNameIncludingDisabled(string name)
        {
            // First try the fast path for active objects
            GameObject activeObj = GameObject.Find(name);
            if (activeObj != null)
                return activeObj;

            // Search all root objects in all loaded scenes (includes disabled)
            foreach (var rootObj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                // Check the root object itself
                if (rootObj.name == name)
                    return rootObj;

                // Search all children (GetComponentsInChildren with includeInactive finds disabled objects)
                foreach (var transform in rootObj.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.gameObject.name == name)
                        return transform.gameObject;
                }
            }

            return null;
        }

        /// <summary>Waits for specified duration then continues to next node.</summary>
        private IEnumerator WaitAndContinue(float duration)
        {
            yield return new WaitForSeconds(duration);
            MoveToNextNode(currentNode.guid, "Output");
        }

        /// <summary>Waits for specified duration then invokes callback.</summary>
        private IEnumerator WaitAndCallback(float duration, System.Action callback)
        {
            yield return new WaitForSeconds(duration);
            callback?.Invoke();
        }

        /// <summary>Callback when choice buttons finish fading in.</summary>
        private void OnChoicesFadedIn()
        {
            state = DialogueState.WaitingForChoice;
        }

        #endregion

        #region Graph Traversal

        /// <summary>
        /// Finds the connection from outputPortName and moves to its target node.
        /// If no connection found, ends the dialogue.
        /// </summary>
        private void MoveToNextNode(string currentGuid, string outputPortName)
        {
            state = DialogueState.ProcessingNode;

            var connections = currentGraph.GetConnectionsFromNode(currentGuid);
            NodeConnectionData nextConnection = null;

            foreach (var conn in connections)
            {
                if (conn.outputPortName == outputPortName)
                {
                    nextConnection = conn;
                    break;
                }
            }

            if (nextConnection != null)
            {
                currentNode = currentGraph.GetNodeByGuid(nextConnection.inputNodeGuid);
                ProcessCurrentNode();
            }
            else
            {
                // No connection from this port - end dialogue
                EndDialogue();
            }
        }

        private BaseNodeData GetNextNodeFromOutput(string currentGuid, string outputPortName)
        {
            var connections = currentGraph.GetConnectionsFromNode(currentGuid);
            foreach (var conn in connections)
            {
                if (conn.outputPortName == outputPortName)
                {
                    return currentGraph.GetNodeByGuid(conn.inputNodeGuid);
                }
            }
            return null;
        }

        /// <summary>Cleans up and fires OnDialogueEnded event.</summary>
        public void EndDialogue()
        {
            state = DialogueState.Ending;
            dialogueUI.Hide();

            // Clean up non-persistent spawned characters
            DespawnCharacterIfNotPersistent();

            state = DialogueState.Inactive;
            currentGraph = null;
            currentNode = null;
            CurrentNodeGuid = null;
            CurrentNodeType = null;

            OnDialogueEnded?.Invoke();
        }

        #region Character Spawning

        /// <summary>Handles character spawning for dialogue nodes.</summary>
        private void HandleCharacterSpawn(DialogueNodeData dialogueNode)
        {
            if (dialogueNode.characterPrefab == null)
                return;

            try
            {
                // Despawn previous non-persistent character
                DespawnCharacterIfNotPersistent();

                // Get spawn container (use this transform if not specified)
                Transform spawnParent = characterSpawnContainer != null ? characterSpawnContainer : transform;

            // Spawn the character prefab
            currentSpawnedCharacter = Instantiate(dialogueNode.characterPrefab, spawnParent);
            currentCharacterPersists = dialogueNode.persistCharacter;

            // Set position
            currentSpawnedCharacter.transform.localPosition = dialogueNode.characterSpawnPosition;

                // Set scale
                Vector3 scale = Vector3.one * dialogueNode.characterScale;
                currentSpawnedCharacter.transform.localScale = scale;

                // Flip using FlipImage component if available, otherwise rely on scale
                var flipImage = currentSpawnedCharacter.GetComponentInChildren<FlipImage>();
                if (flipImage != null)
                {
                    flipImage.SyncBaseScaleFromTransform();
                    flipImage.ApplyFlip(dialogueNode.flipCharacter);
                }
                else if (dialogueNode.flipCharacter)
                {
                    Vector3 flippedScale = currentSpawnedCharacter.transform.localScale;
                    flippedScale.x *= -1f;
                    currentSpawnedCharacter.transform.localScale = flippedScale;
                }

                // Set character sprite if provided
                if (dialogueNode.characterSprite != null)
                {
                    // Try to find SpriteRenderer and set the sprite
                    SpriteRenderer spriteRenderer = currentSpawnedCharacter.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = dialogueNode.characterSprite;
                }
                else
                {
                    // Also try UI.Image on root or children
                    Image uiImage = currentSpawnedCharacter.GetComponent<Image>();
                    if (uiImage == null)
                        uiImage = currentSpawnedCharacter.GetComponentInChildren<Image>();

                    if (uiImage != null)
                    {
                        uiImage.sprite = dialogueNode.characterSprite;
                    }
                }
            }

            EnsureCharacterHoverFeedback(currentSpawnedCharacter);
        }
            catch (Exception ex)
            {
                Debug.LogError($"DialogueManager: Exception while spawning '{dialogueNode.characterPrefab.name}' for '{dialogueNode.speakerName}': {ex}");
            }
        }

        /// <summary>Despawns the current character if it's not set to persist.</summary>
        private void DespawnCharacterIfNotPersistent()
        {
            if (currentSpawnedCharacter != null && !currentCharacterPersists)
            {
                Destroy(currentSpawnedCharacter);
                currentSpawnedCharacter = null;
            }
        }

        private void EnsureCharacterHoverFeedback(GameObject character)
        {
            if (character == null)
                return;

            if (character.GetComponent<CharacterHoverFeedback>() == null)
            {
                character.AddComponent<CharacterHoverFeedback>();
            }
        }

        /// <summary>Manually despawn any spawned character (even if persistent).</summary>
        public void DespawnCurrentCharacter()
        {
            if (currentSpawnedCharacter != null)
            {
                Destroy(currentSpawnedCharacter);
                currentSpawnedCharacter = null;
                currentCharacterPersists = false;
            }
        }

        #endregion

        #endregion
    }
}
