using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class EventNode : BaseDialogueNode
    {
        private EnumField eventTypeField;
        private TextField eventKeyField;
        private TextField targetObjectField;
        private ObjectField backgroundSpriteField;
        private ObjectField audioClipField;
        private FloatField transitionDurationField;
        private Toggle waitForCompletionField;

        private VisualElement animationContainer;
        private VisualElement backgroundContainer;
        private VisualElement audioContainer;
        private VisualElement customContainer;
        private VisualElement revealCharacterContainer;
        private VisualElement hideCharacterContainer;
        private TextField characterNameField;
        private TextField hideCharacterNameField;
        private FloatField hideFadeDurationField;
        private TextField customKeyField;

        public DialogueEventType EventType => (DialogueEventType)(eventTypeField?.value ?? DialogueEventType.Custom);
        public string EventKey
        {
            get
            {
                // For Custom and ReturnToMainMenu events, use the customKeyField value
                var eventType = EventType;
                if ((eventType == DialogueEventType.Custom || eventType == DialogueEventType.ReturnToMainMenu) && customKeyField != null)
                {
                    return customKeyField.value ?? "";
                }
                return eventKeyField?.value ?? "";
            }
        }
        public string TargetObjectName => targetObjectField?.value ?? "";
        public Sprite BackgroundSprite => backgroundSpriteField?.value as Sprite;
        public AudioClip AudioClip => audioClipField?.value as AudioClip;
        public float TransitionDuration => transitionDurationField?.value ?? 0f;
        public bool WaitForCompletion => waitForCompletionField?.value ?? false;
        public string CharacterName => characterNameField?.value ?? "";
        public string HideCharacterName => hideCharacterNameField?.value ?? "";
        public float HideFadeDuration => hideFadeDurationField?.value ?? 0.5f;

        public EventNode()
        {
            title = "Event";
            AddToClassList("event-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();
            UpdateFieldVisibility();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreateFields()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.minWidth = 200;

            eventTypeField = new EnumField("Event Type", DialogueEventType.Custom);
            eventTypeField.RegisterValueChangedCallback(evt => UpdateFieldVisibility());
            container.Add(eventTypeField);

            // Animation container
            animationContainer = new VisualElement();
            animationContainer.style.marginTop = 4;

            var animLabel = new Label("Animation Settings");
            animLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            animLabel.style.marginBottom = 2;
            animationContainer.Add(animLabel);

            targetObjectField = new TextField("Target Object");
            animationContainer.Add(targetObjectField);

            eventKeyField = new TextField("Animation Name");
            eventKeyField.style.marginTop = 2;
            animationContainer.Add(eventKeyField);

            container.Add(animationContainer);

            // Background container
            backgroundContainer = new VisualElement();
            backgroundContainer.style.marginTop = 4;

            backgroundSpriteField = new ObjectField("Background")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false
            };
            backgroundContainer.Add(backgroundSpriteField);

            transitionDurationField = new FloatField("Transition Duration");
            transitionDurationField.value = 0.5f;
            transitionDurationField.style.marginTop = 2;
            backgroundContainer.Add(transitionDurationField);

            container.Add(backgroundContainer);

            // Audio container
            audioContainer = new VisualElement();
            audioContainer.style.marginTop = 4;

            audioClipField = new ObjectField("Audio Clip")
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };
            audioContainer.Add(audioClipField);

            container.Add(audioContainer);

            // Custom container
            customContainer = new VisualElement();
            customContainer.style.marginTop = 4;

            customKeyField = new TextField("Event Key");
            customContainer.Add(customKeyField);
            // Sync customKeyField with eventKeyField bidirectionally
            customKeyField.RegisterValueChangedCallback(evt =>
            {
                if (EventType == DialogueEventType.Custom || EventType == DialogueEventType.ReturnToMainMenu)
                    eventKeyField.value = evt.newValue;
            });

            container.Add(customContainer);

            // Reveal Character container
            revealCharacterContainer = new VisualElement();
            revealCharacterContainer.style.marginTop = 4;

            var revealLabel = new Label("Reveal Character");
            revealLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            revealLabel.style.marginBottom = 2;
            revealCharacterContainer.Add(revealLabel);

            characterNameField = new TextField("Character Name");
            characterNameField.tooltip = "Name of the character to reveal (matches CharacterData.characterName)";
            revealCharacterContainer.Add(characterNameField);

            container.Add(revealCharacterContainer);

            // Hide Character container
            hideCharacterContainer = new VisualElement();
            hideCharacterContainer.style.marginTop = 4;

            var hideLabel = new Label("Hide Character");
            hideLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hideLabel.style.marginBottom = 2;
            hideCharacterContainer.Add(hideLabel);

            hideCharacterNameField = new TextField("Character Name");
            hideCharacterNameField.tooltip = "Name of the character to hide (matches CharacterData.characterName)";
            hideCharacterContainer.Add(hideCharacterNameField);

            hideFadeDurationField = new FloatField("Fade Duration");
            hideFadeDurationField.value = 0.5f;
            hideFadeDurationField.tooltip = "Duration in seconds for the fade out effect";
            hideFadeDurationField.style.marginTop = 2;
            hideCharacterContainer.Add(hideFadeDurationField);

            container.Add(hideCharacterContainer);

            // Wait for completion (shown for all types)
            waitForCompletionField = new Toggle("Wait For Completion");
            waitForCompletionField.style.marginTop = 8;
            container.Add(waitForCompletionField);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        private void UpdateFieldVisibility()
        {
            var eventType = EventType;

            animationContainer.style.display = DisplayStyle.None;
            backgroundContainer.style.display = DisplayStyle.None;
            audioContainer.style.display = DisplayStyle.None;
            customContainer.style.display = DisplayStyle.None;
            revealCharacterContainer.style.display = DisplayStyle.None;
            hideCharacterContainer.style.display = DisplayStyle.None;

            switch (eventType)
            {
                case DialogueEventType.PlayAnimation:
                    animationContainer.style.display = DisplayStyle.Flex;
                    break;
                case DialogueEventType.ChangeBackground:
                    backgroundContainer.style.display = DisplayStyle.Flex;
                    break;
                case DialogueEventType.PlayMusic:
                case DialogueEventType.PlaySoundEffect:
                    audioContainer.style.display = DisplayStyle.Flex;
                    break;
                case DialogueEventType.StopMusic:
                    // No additional fields needed
                    break;
                case DialogueEventType.RevealCharacter:
                    revealCharacterContainer.style.display = DisplayStyle.Flex;
                    break;
                case DialogueEventType.HideCharacter:
                    hideCharacterContainer.style.display = DisplayStyle.Flex;
                    break;
                case DialogueEventType.EndPhoneCall:
                    // No additional fields needed - just triggers phone call end
                    break;
                case DialogueEventType.ReturnToMainMenu:
                    // Uses eventKey for scene name (optional, defaults to "MainMenu")
                    customContainer.style.display = DisplayStyle.Flex;
                    break;
                case DialogueEventType.Custom:
                    customContainer.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var eventData = data as EventNodeData;
            if (eventData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            eventTypeField.value = eventData.eventType;
            eventKeyField.value = eventData.eventKey ?? "";
            targetObjectField.value = eventData.targetObjectName ?? "";
            backgroundSpriteField.value = eventData.backgroundSprite;
            audioClipField.value = eventData.audioClip;
            transitionDurationField.value = eventData.transitionDuration;
            waitForCompletionField.value = eventData.waitForCompletion;
            characterNameField.value = eventData.characterName ?? "";
            hideCharacterNameField.value = eventData.hideCharacterName ?? "";
            hideFadeDurationField.value = eventData.hideFadeDuration > 0 ? eventData.hideFadeDuration : 0.5f;
            customKeyField.value = eventData.eventKey ?? "";

            UpdateFieldVisibility();

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new EventNodeData
            {
                guid = Guid,
                nodeType = "Event",
                position = GetPosition().position,
                eventType = EventType,
                eventKey = EventKey,
                targetObjectName = TargetObjectName,
                backgroundSprite = BackgroundSprite,
                audioClip = AudioClip,
                transitionDuration = TransitionDuration,
                waitForCompletion = WaitForCompletion,
                characterName = CharacterName,
                hideCharacterName = HideCharacterName,
                hideFadeDuration = HideFadeDuration
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
