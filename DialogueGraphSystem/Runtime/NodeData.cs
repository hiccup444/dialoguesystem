using System;
using System.Collections.Generic;
using UnityEngine;

namespace VNWinter.DialogueGraph
{
    /// <summary>
    /// Base class for all dialogue node data. Contains common properties shared by all node types.
    /// Serialized as part of DialogueGraphAsset for runtime playback.
    /// </summary>
    [Serializable]
    public abstract class BaseNodeData
    {
        /// <summary>Unique identifier for this node, used for connections and lookups.</summary>
        public string guid;

        /// <summary>Type identifier string (e.g., "Dialogue", "Choice") for polymorphic deserialization.</summary>
        public string nodeType;

        /// <summary>Position in the graph editor canvas. Not used at runtime.</summary>
        public Vector2 position;

        /// <summary>Optional notes for designers/writers. Not displayed in-game.</summary>
        [TextArea(2, 4)]
        public string designerNotes;

        /// <summary>Whether the node is expanded in the editor. Editor-only, not used at runtime.</summary>
        public bool isExpanded = true;
    }

    /// <summary>
    /// Entry point node for a dialogue graph. Each graph should have exactly one Start node.
    /// The dialogue system begins execution from this node.
    /// </summary>
    [Serializable]
    public class StartNodeData : BaseNodeData
    {
        public StartNodeData()
        {
            nodeType = "Start";
        }
    }

    /// <summary>
    /// Standard dialogue node displaying speaker name, text, and optional portrait.
    /// Supports voice acting clips and ambient audio for atmosphere.
    /// </summary>
    [Serializable]
    public class DialogueNodeData : BaseNodeData
    {
        /// <summary>Character name displayed above dialogue text.</summary>
        public string speakerName;

        /// <summary>The dialogue text shown to the player.</summary>
        [TextArea(3, 10)]
        public string dialogueText;

    /// <summary>Optional character portrait sprite displayed alongside dialogue.</summary>
    public Sprite portrait;

        /// <summary>Flag that determines whether the portrait panel is shown even if a portrait exists.</summary>
        public bool showPortraitPanel = true;

        /// <summary>Flag that determines whether the speaker name panel is shown.</summary>
        public bool showSpeakerName = true;

        /// <summary>If true, this dialogue represents internal thoughts/narration. Thoughts are displayed in italics and can use a different dialogue box sprite.</summary>
        public bool isThought;

        /// <summary>If true, requires player to click to advance. If false, auto-advances to next node after typewriter completes (useful for final dialogue).</summary>
        public bool requireInteraction = true;

        [Header("Portrait Positioning")]
        /// <summary>Use the alternate PortraitImageFront GameObject instead of the default PortraitImage.</summary>
        public bool useEndingPortraitFront = false;

        /// <summary>Which front portrait to use (1-3). Maps to PortraitImageFront-1, PortraitImageFront-2, PortraitImageFront-3.</summary>
        [Range(1, 3)]
        public int endingPortraitFrontIndex = 1;

        /// <summary>Enable custom portrait positioning. If false, uses default position from UI prefab.</summary>
        public bool useCustomPositioning = false;

        /// <summary>Screen position for the portrait panel (anchored position in pixels).</summary>
        public Vector2 portraitPosition = Vector2.zero;

        /// <summary>Scale multiplier for the portrait (1.0 = normal size, 2.0 = twice as big).</summary>
        [Range(0.1f, 3f)]
        public float portraitScale = 1f;

        /// <summary>Anchor point for the portrait (0,0 = bottom-left, 0.5,0.5 = center, 1,1 = top-right).</summary>
        public Vector2 portraitAnchor = new Vector2(0.5f, 0.5f);

        /// <summary>Custom width/height for the portrait image. Only used when useCustomPositioning is true.</summary>
        public Vector2 portraitSize = new Vector2(2048f, 2048f);

        /// <summary>Flip the portrait horizontally.</summary>
        public bool flipPortrait = false;

        [Header("Emotion Swap")]
        /// <summary>Enable swapping to a different portrait sprite during this dialogue.</summary>
        public bool emotionSwapEnabled = false;

        /// <summary>When to swap to the emotion sprite.</summary>
        public EmotionSwapTiming emotionSwapTiming = EmotionSwapTiming.Immediately;

        /// <summary>The emotion type to swap to (looks up portraitId from CharacterData).</summary>
        public EmotionType emotionType = EmotionType.Neutral;

        [Header("Portrait Sway")]
        /// <summary>Enable a simple sway animation on the portrait.</summary>
        public bool swayEnabled = false;

        /// <summary>Motion path used when swaying the portrait.</summary>
        public SwayPattern swayPattern = SwayPattern.LeftRight;

        /// <summary>Speed multiplier for the sway animation.</summary>
        public float swaySpeed = 1f;

        /// <summary>Movement distance for the sway animation (in anchored position pixels).</summary>
        public float swayIntensity = 10f;

        /// <summary>Enable a second simultaneous sway animation.</summary>
        public bool sway2Enabled = false;

        /// <summary>Motion path for the second sway.</summary>
        public SwayPattern sway2Pattern = SwayPattern.UpDown;

        /// <summary>Speed multiplier for the second sway.</summary>
        public float sway2Speed = 1f;

        /// <summary>Movement distance for the second sway (in anchored position pixels).</summary>
        public float sway2Intensity = 10f;

        [Header("Portrait Rotation")]
        /// <summary>Enable oscillating rotation for the portrait.</summary>
        public bool rotationEnabled = false;

        /// <summary>Axis to rotate around.</summary>
        public RotationAxis rotationAxis = RotationAxis.Z;

        /// <summary>Minimum angle in degrees for the oscillation.</summary>
        public float rotationMinAngle = -5f;

        /// <summary>Maximum angle in degrees for the oscillation.</summary>
        public float rotationMaxAngle = 5f;

        /// <summary>Speed multiplier for the rotation oscillation.</summary>
        public float rotationSpeed = 1f;

        [Header("Portrait Scale Pulse")]
        /// <summary>Enable oscillating scale between two values.</summary>
        public bool scaleEnabled = false;

        /// <summary>Minimum scale multiplier.</summary>
        public float scaleMin = 1f;

        /// <summary>Maximum scale multiplier.</summary>
        public float scaleMax = 1.1f;

        /// <summary>Speed multiplier for the scale oscillation.</summary>
        public float scaleSpeed = 1f;

        [Header("Speaking Scale Effect")]
        /// <summary>Enable a subtle scale pulse while dialogue is typing to simulate speaking.</summary>
        public bool speakingScaleEnabled = true;

        [Header("Character Spawn")]
        /// <summary>Optional character prefab to spawn in the scene when this dialogue plays.</summary>
        public GameObject characterPrefab;

        /// <summary>Character sprite (full body) to pass to the spawned prefab.</summary>
        public Sprite characterSprite;

        /// <summary>World position where character spawns.</summary>
        public Vector2 characterSpawnPosition = Vector2.zero;

        /// <summary>Scale for spawned character (1.0 = normal size).</summary>
        [Range(0.1f, 5f)]
        public float characterScale = 1f;

        /// <summary>Flip the spawned character horizontally.</summary>
        public bool flipCharacter = false;

        /// <summary>If true, character persists until another dialogue node despawns it. If false, despawns when dialogue ends.</summary>
        public bool persistCharacter = false;

        [Header("Audio")]
        /// <summary>Optional voice acting audio clip for this line.</summary>
        public AudioClip voiceClip;

        /// <summary>Optional ambient/background audio that plays during this dialogue.</summary>
        public AudioClip ambientAudio;

        /// <summary>If true, ambient audio loops until changed by another node.</summary>
        public bool loopAmbient;

        /// <summary>Delay in seconds before voice clip starts playing.</summary>
        public float voiceDelay;

        public DialogueNodeData()
        {
            nodeType = "Dialogue";
            showPortraitPanel = true;
            showSpeakerName = true;
            useEndingPortraitFront = false;
            endingPortraitFrontIndex = 1;
            useCustomPositioning = false;
            portraitPosition = Vector2.zero;
            portraitScale = 1f;
            portraitAnchor = new Vector2(0.5f, 0.5f);
            portraitSize = new Vector2(2048f, 2048f);
            flipPortrait = false;
            emotionSwapEnabled = false;
            emotionSwapTiming = EmotionSwapTiming.Immediately;
            emotionType = EmotionType.Neutral;
            swayEnabled = false;
            swayPattern = SwayPattern.LeftRight;
            swaySpeed = 1f;
            swayIntensity = 10f;
            sway2Enabled = false;
            sway2Pattern = SwayPattern.UpDown;
            sway2Speed = 1f;
            sway2Intensity = 10f;
            rotationEnabled = false;
            rotationAxis = RotationAxis.Z;
            rotationMinAngle = -5f;
            rotationMaxAngle = 5f;
            rotationSpeed = 1f;
            scaleEnabled = false;
            scaleMin = 1f;
            scaleMax = 1.1f;
            scaleSpeed = 1f;
            speakingScaleEnabled = true;
            characterPrefab = null;
            characterSprite = null;
            characterSpawnPosition = Vector2.zero;
            characterScale = 1f;
            flipCharacter = false;
            persistCharacter = false;
        }
    }

    /// <summary>
    /// Represents a single choice option within a ChoiceNodeData.
    /// </summary>
    [Serializable]
    public class ChoiceData
    {
        /// <summary>Unique ID for this choice, used to identify the output port connection.</summary>
        public string choiceGuid;

        /// <summary>Text displayed on the choice button.</summary>
        public string choiceText;

        /// <summary>Conversation point value awarded for selecting this choice.</summary>
        public int conversationPoints;
    }

    /// <summary>
    /// Branching node that presents multiple choices to the player.
    /// Each choice connects to a different dialogue path.
    /// </summary>
    [Serializable]
    public class ChoiceNodeData : BaseNodeData
    {
        /// <summary>List of choices presented to the player. Each has its own output connection.</summary>
        public List<ChoiceData> choices = new List<ChoiceData>();

        public ChoiceNodeData()
        {
            nodeType = "Choice";
        }
    }

    /// <summary>
    /// Terminal node marking the end of a dialogue conversation.
    /// When reached, DialogueManager fires OnDialogueEnd and cleans up.
    /// </summary>
    [Serializable]
    public class EndNodeData : BaseNodeData
    {
        public EndNodeData()
        {
            nodeType = "End";
        }
    }

    /// <summary>
    /// Random branching node that randomly selects one of up to 4 output paths.
    /// Use to add variety and unpredictability to dialogue or events.
    /// </summary>
    [Serializable]
    public class RandomNodeData : BaseNodeData
    {
        /// <summary>GUID for output 1 port connection.</summary>
        public string output1PortGuid;

        /// <summary>GUID for output 2 port connection.</summary>
        public string output2PortGuid;

        /// <summary>GUID for output 3 port connection (optional).</summary>
        public string output3PortGuid;

        /// <summary>GUID for output 4 port connection (optional).</summary>
        public string output4PortGuid;

        public RandomNodeData()
        {
            nodeType = "Random";
        }
    }

    /// <summary>
    /// Supported variable types for the GlobalVariables system.
    /// </summary>
    public enum VariableType
    {
        Int,
        Bool
    }

    /// <summary>
    /// Operations available for integer variables.
    /// </summary>
    public enum IntOperation
    {
        /// <summary>Replace the current value entirely.</summary>
        Set,
        /// <summary>Add to the current value.</summary>
        Add,
        /// <summary>Subtract from the current value.</summary>
        Subtract
    }

    /// <summary>
    /// Operations available for boolean variables.
    /// </summary>
    public enum BoolOperation
    {
        /// <summary>Set to a specific true/false value.</summary>
        Set,
        /// <summary>Flip the current value (true becomes false, false becomes true).</summary>
        Toggle
    }

    /// <summary>
    /// Comparison operators for conditional branching on integer values.
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    /// Node that modifies GlobalVariables values during dialogue.
    /// Use to track player choices, increment counters, set flags, etc.
    /// </summary>
    [Serializable]
    public class VariableNodeData : BaseNodeData
    {
        /// <summary>Whether we're modifying an int or bool variable.</summary>
        public VariableType variableType = VariableType.Int;

        /// <summary>Name/key of the variable in GlobalVariables.</summary>
        public string variableName;

        /// <summary>Operation to perform on int variables.</summary>
        public IntOperation intOperation = IntOperation.Set;

        /// <summary>Value used for int operations (set to X, add X, subtract X).</summary>
        public int intValue;

        /// <summary>Operation to perform on bool variables.</summary>
        public BoolOperation boolOperation = BoolOperation.Set;

        /// <summary>Value used for bool Set operation.</summary>
        public bool boolValue;

        public VariableNodeData()
        {
            nodeType = "Variable";
        }
    }

    /// <summary>
    /// Conditional branching node that checks GlobalVariables and routes to True or False output.
    /// Use to create dialogue paths based on player choices, stats, or flags.
    /// </summary>
    [Serializable]
    public class ConditionalNodeData : BaseNodeData
    {
        /// <summary>Whether we're checking an int or bool variable.</summary>
        public VariableType variableType = VariableType.Bool;

        /// <summary>Name/key of the variable to check in GlobalVariables.</summary>
        public string variableName;

        /// <summary>Comparison operator for int variables (==, !=, >, <, >=, <=).</summary>
        public ComparisonOperator comparison = ComparisonOperator.Equal;

        /// <summary>Value to compare against for int variables.</summary>
        public int compareValue;

        /// <summary>For bool variables: what value we're checking for (true or false).</summary>
        public bool expectedBoolValue = true;

        /// <summary>Output port GUID for the "condition is true" path.</summary>
        public string truePortGuid;

        /// <summary>Output port GUID for the "condition is false" path.</summary>
        public string falsePortGuid;

        public ConditionalNodeData()
        {
            nodeType = "Conditional";
            truePortGuid = System.Guid.NewGuid().ToString();
            falsePortGuid = System.Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Types of events that can be triggered by an EventNode during dialogue.
    /// </summary>
    public enum DialogueEventType
    {
        /// <summary>Trigger an animation on a target GameObject.</summary>
        PlayAnimation,
        /// <summary>Change the scene background image.</summary>
        ChangeBackground,
        /// <summary>Start playing background music.</summary>
        PlayMusic,
        /// <summary>Stop currently playing music.</summary>
        StopMusic,
        /// <summary>Play a one-shot sound effect.</summary>
        PlaySoundEffect,
        /// <summary>Reveal a hidden character in the current room by character name.</summary>
        RevealCharacter,
        /// <summary>Hide a visible character in the current room by fading out.</summary>
        HideCharacter,
        /// <summary>End an active phone call, returning the phone to screen.</summary>
        EndPhoneCall,
        /// <summary>Fade to main menu scene.</summary>
        ReturnToMainMenu,
        /// <summary>Fire a custom event for game-specific handling via eventKey.</summary>
        Custom
    }

    /// <summary>
    /// Event/signal node that triggers game events during dialogue.
    /// Handles animations, background changes, music, sound effects, and custom events.
    /// Subscribe to DialogueManager.OnDialogueEvent to handle these in your game code.
    /// </summary>
    [Serializable]
    public class EventNodeData : BaseNodeData
    {
        /// <summary>Type of event to trigger.</summary>
        public DialogueEventType eventType;

        /// <summary>Context-dependent key: animation name, custom event ID, etc.</summary>
        public string eventKey;

        /// <summary>Optional name of target GameObject for animations.</summary>
        public string targetObjectName;

        /// <summary>Sprite for ChangeBackground events.</summary>
        public Sprite backgroundSprite;

        /// <summary>Audio clip for PlayMusic or PlaySoundEffect events.</summary>
        public AudioClip audioClip;

        /// <summary>Duration for transitions (e.g., background fade time).</summary>
        public float transitionDuration;

        /// <summary>If true, dialogue waits until event completes before continuing.</summary>
        public bool waitForCompletion;

        /// <summary>Character name for RevealCharacter events (matches CharacterData.characterName).</summary>
        public string characterName;

        /// <summary>Character name for HideCharacter events (matches CharacterData.characterName).</summary>
        public string hideCharacterName;

        /// <summary>Fade duration for HideCharacter events.</summary>
        public float hideFadeDuration = 0.5f;

        public EventNodeData()
        {
            nodeType = "Event";
        }
    }

    [Serializable]
    public class RoomTransitionNodeData : BaseNodeData
    {
        /// <summary>Scene name or identifier to load when this node executes.</summary>
        public string targetSceneName;

        /// <summary>Whether to wait for a transition to finish before continuing.
        /// Assign DialogueManager.IsRoomTransitioning delegate to enable this check.</summary>
        public bool waitForTransitionCompletion = true;

        /// <summary>Additional delay after the transition finishes before moving on.</summary>
        public float postTransitionDelay = 0f;

        public RoomTransitionNodeData()
        {
            nodeType = "RoomTransition";
            targetSceneName = string.Empty;
            waitForTransitionCompletion = true;
            postTransitionDelay = 0f;
        }
    }

    [Serializable]
    public class FadeNodeData : BaseNodeData
    {
        /// <summary>Type of fade effect to perform.</summary>
        public enum FadeType
        {
            FadeOut,    // Fade to color (screen goes dark)
            FadeIn,     // Fade from color (clears to game)
            FadeToColor, // Fade to specific color
            SolidColor  // Display solid color for duration (no fade transition)
        }

        /// <summary>Type of fade effect to perform.</summary>
        public FadeType fadeType = FadeType.FadeOut;

        /// <summary>Target color to fade to (typically black).</summary>
        public Color fadeColor = Color.black;

        /// <summary>Duration of the fade effect in seconds.</summary>
        public float fadeDuration = 1f;

        /// <summary>If true, dialogue waits until fade completes before continuing.</summary>
        public bool waitForCompletion = true;

        public FadeNodeData()
        {
            nodeType = "Fade";
        }
    }

    [Serializable]
    public class GraphTransitionNodeData : BaseNodeData
    {
        /// <summary>Dialogue graph asset to start when this node runs.</summary>
        public DialogueGraphAsset targetGraph;

        public GraphTransitionNodeData()
        {
            nodeType = "GraphTransition";
            targetGraph = null;
        }
    }

    /// <summary>
    /// Control node that blocks auto-triggered dialogue but allows manual interactions.
    /// Use to prevent room auto-trigger from continuing past a certain point while still
    /// allowing player-initiated dialogue to proceed normally.
    /// </summary>
    [Serializable]
    public class PreventAutoTriggerNodeData : BaseNodeData
    {
        public PreventAutoTriggerNodeData()
        {
            nodeType = "PreventAutoTrigger";
        }
    }

    /// <summary>
    /// Node that replaces an image on a scene object by name.
    /// Finds a GameObject in the scene and replaces its Image component's sprite.
    /// </summary>
    [Serializable]
    public class SceneImageNodeData : BaseNodeData
    {
        /// <summary>Name of the GameObject in the scene to find.</summary>
        public string targetObjectName;

        /// <summary>Sprite to set on the target object's Image component.</summary>
        public Sprite imageSprite;

        /// <summary>Opacity/alpha value for the image (0 = transparent, 1 = fully opaque).</summary>
        [Range(0f, 1f)]
        public float opacity = 1f;

        public SceneImageNodeData()
        {
            nodeType = "SceneImage";
            opacity = 1f;
        }
    }

    /// <summary>
    /// Audio channel types that can be targeted by AudioNode.
    /// </summary>
    public enum AudioChannelType
    {
        /// <summary>Background music channel.</summary>
        Music,
        /// <summary>Sound effects channel.</summary>
        SFX,
        /// <summary>Voice-over channel.</summary>
        Voice
    }

    /// <summary>
    /// Node that controls audio playback and volume with smooth fade transitions.
    /// Can play a new track or adjust volume on the currently playing track.
    /// </summary>
    [Serializable]
    public class AudioNodeData : BaseNodeData
    {
        /// <summary>Which audio channel to modify (Music, SFX, or Voice).</summary>
        public AudioChannelType channelType = AudioChannelType.Music;

        /// <summary>Audio clip to play. If null, only adjusts volume of currently playing track.</summary>
        public AudioClip audioClip;

        /// <summary>Whether the audio should loop (typically true for music, false for SFX).</summary>
        public bool loop = true;

        /// <summary>Target volume level (0 = silent, 1 = full volume).</summary>
        [Range(0f, 1f)]
        public float targetVolume = 1f;

        /// <summary>Duration of the volume fade in seconds.</summary>
        public float fadeDuration = 1f;

        /// <summary>If true, waits for the fade to complete before continuing to the next node.</summary>
        public bool waitForCompletion = false;

        public AudioNodeData()
        {
            nodeType = "Audio";
            channelType = AudioChannelType.Music;
            audioClip = null;
            loop = true;
            targetVolume = 1f;
            fadeDuration = 1f;
            waitForCompletion = false;
        }
    }

    /// <summary>
    /// Node that stops audio playback on a specified channel.
    /// Can optionally fade out before stopping.
    /// </summary>
    [Serializable]
    public class StopSoundNodeData : BaseNodeData
    {
        /// <summary>Which audio channel to stop (Music, SFX, or Voice).</summary>
        public AudioChannelType channelType = AudioChannelType.Music;

        /// <summary>Duration of the fade out before stopping (0 for instant stop).</summary>
        public float fadeOutDuration = 0f;

        /// <summary>If true, waits for the fade out to complete before continuing to the next node.</summary>
        public bool waitForCompletion = false;

        public StopSoundNodeData()
        {
            nodeType = "StopSound";
            channelType = AudioChannelType.Music;
            fadeOutDuration = 0f;
            waitForCompletion = false;
        }
    }

    /// <summary>
    /// Node that saves the current music playback time to a GlobalVariable.
    /// Used to persist music position across scene transitions.
    /// </summary>
    [Serializable]
    public class SaveAudioPlaybackNodeData : BaseNodeData
    {
        public SaveAudioPlaybackNodeData()
        {
            nodeType = "SaveAudioPlayback";
        }
    }

    /// <summary>
    /// Node that prevents automatic last_character tracking for this dialogue graph.
    /// Place immediately after Start node to skip the workaround that sets the speaker as last character.
    /// </summary>
    [Serializable]
    public class NotLastCharacterNodeData : BaseNodeData
    {
        public NotLastCharacterNodeData()
        {
            nodeType = "NotLastCharacter";
        }
    }

    /// <summary>
    /// Node that triggers the frosting minigame by calling StartFrosting() on FrostingController.
    /// </summary>
    [Serializable]
    public class StartFrostingNodeData : BaseNodeData
    {
        public StartFrostingNodeData()
        {
            nodeType = "StartFrosting";
        }
    }

    /// <summary>
    /// Timing options for emotion/expression sprite swaps during dialogue.
    /// </summary>
    public enum EmotionSwapTiming
    {
        /// <summary>Swap sprite immediately when the dialogue node begins.</summary>
        Immediately,
        /// <summary>Swap sprite after the dialogue text finishes typing.</summary>
        AfterDialogue,
        /// <summary>Swap sprite halfway through the typing sequence.</summary>
        PartialDialogue
    }

    /// <summary>
    /// Emotion types for character portrait swaps during dialogue.
    /// Maps to portraitId in CharacterData.portraits.
    /// </summary>
    public enum EmotionType
    {
        /// <summary>Neutral expression (portraitId: "neutral")</summary>
        Neutral,
        /// <summary>Positive expression (portraitId: "positive")</summary>
        Positive,
        /// <summary>Negative expression (portraitId: "negative")</summary>
        Negative
    }

    /// <summary>
    /// Sway pattern types for portrait animation.
    /// </summary>
    public enum SwayPattern
    {
        LeftRight,
        UpDown,
        Circular,
        Figure8
    }

    /// <summary>
    /// Axes available for portrait rotation.
    /// </summary>
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    /// <summary>
    /// Represents a connection between two nodes in the dialogue graph.
    /// Stored separately from nodes to maintain graph topology.
    /// </summary>
    [Serializable]
    public class NodeConnectionData
    {
        /// <summary>GUID of the source node (where the connection starts).</summary>
        public string outputNodeGuid;

        /// <summary>Name of the output port on the source node.</summary>
        public string outputPortName;

        /// <summary>GUID of the destination node (where the connection ends).</summary>
        public string inputNodeGuid;

        /// <summary>Name of the input port on the destination node.</summary>
        public string inputPortName;
    }

    /// <summary>
    /// Font size options for sticky notes in the graph editor.
    /// </summary>
    public enum StickyNoteFontSize
    {
        Small,
        Normal,
        Large,
        ExtraLarge
    }

    /// <summary>
    /// Serializable data for sticky note annotations in the dialogue graph.
    /// Sticky notes are editor-only elements for documentation and reminders.
    /// </summary>
    [Serializable]
    public class StickyNoteData
    {
        /// <summary>Unique identifier for this sticky note.</summary>
        public string guid;

        /// <summary>Title/header text of the sticky note.</summary>
        public string title;

        /// <summary>Main content/body text of the sticky note.</summary>
        [TextArea(3, 10)]
        public string content;

        /// <summary>Font size for the content text.</summary>
        public StickyNoteFontSize fontSize;

        /// <summary>Background color of the sticky note.</summary>
        public Color backgroundColor = new Color(1f, 0.95f, 0.7f, 1f); // Default yellow

        /// <summary>Position in the graph editor canvas.</summary>
        public Vector2 position;

        /// <summary>Width and height of the sticky note.</summary>
        public Vector2 size;
    }
}
