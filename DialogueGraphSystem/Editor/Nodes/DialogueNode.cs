using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class DialogueNode : BaseDialogueNode
    {
        private TextField speakerField;
        private TextField dialogueField;
        private ObjectField portraitField;
        private Toggle showPortraitToggle;
        private Toggle showSpeakerNameToggle;
        private Toggle isThoughtToggle;
        private Toggle requireInteractionToggle;
        private Toggle flipPortraitToggle;

        // Emotion swap fields
        private Toggle emotionSwapEnabledToggle;
        private EnumField emotionSwapTimingField;
        private EnumField emotionTypeField;

        // Portrait positioning fields
        private Toggle useEndingPortraitFrontToggle;
        private SliderInt endingPortraitFrontIndexField;
        private Toggle useCustomPositioningToggle;
        private Vector2Field portraitPositionField;
        private Slider portraitScaleField;
        private Vector2Field portraitAnchorField;
        private Vector2Field portraitSizeField;
        private Toggle flipPortraitField;
        private Foldout portraitFoldout;
        private bool isSyncingFlipPortrait;
        private Toggle swayEnabledToggle;
        private EnumField swayPatternField;
        private Slider swaySpeedField;
        private Slider swayIntensityField;
        private Toggle sway2EnabledToggle;
        private EnumField sway2PatternField;
        private Slider sway2SpeedField;
        private Slider sway2IntensityField;
        private Toggle rotationEnabledToggle;
        private EnumField rotationAxisField;
        private FloatField rotationMinAngleField;
        private FloatField rotationMaxAngleField;
        private Slider rotationSpeedField;
        private Toggle scaleEnabledToggle;
        private Slider scaleMinField;
        private Slider scaleMaxField;
        private Slider scaleSpeedField;
        private Toggle speakingScaleEnabledToggle;

        // Character spawn fields
        private ObjectField characterPrefabField;
        private ObjectField characterSpriteField;
        private Vector2Field characterSpawnPositionField;
        private Slider characterScaleField;
        private Toggle flipCharacterField;
        private Toggle persistCharacterField;
        private Foldout characterSpawnFoldout;

        // Audio fields
        private ObjectField voiceClipField;
        private ObjectField ambientAudioField;
        private Toggle loopAmbientField;
        private FloatField voiceDelayField;
        private Foldout audioFoldout;

        public string SpeakerName => speakerField?.value ?? "";
        public string DialogueText => dialogueField?.value ?? "";
        public Sprite Portrait => portraitField?.value as Sprite;
        public AudioClip VoiceClip => voiceClipField?.value as AudioClip;
        public AudioClip AmbientAudio => ambientAudioField?.value as AudioClip;
        public bool LoopAmbient => loopAmbientField?.value ?? false;
        public float VoiceDelay => voiceDelayField?.value ?? 0f;

        public void SetSpeakerAndPortrait(string speaker, Sprite portrait)
        {
            if (speakerField != null && string.IsNullOrEmpty(speakerField.value))
            {
                speakerField.value = speaker ?? "";
            }
            if (portraitField != null && portraitField.value == null)
            {
                portraitField.value = portrait;
            }
        }

        public DialogueNode()
        {
            title = "Dialogue";
            AddToClassList("dialogue-node");
        }

        private void UpdateNodeStyling()
        {
            bool isBonnie = string.Equals(speakerField?.value?.Trim(), "Bonnie", System.StringComparison.OrdinalIgnoreCase);
            bool isThought = isThoughtToggle?.value ?? false;

            RemoveFromClassList("dialogue-node-bonnie");
            RemoveFromClassList("dialogue-node-bonnie-thought");

            if (isBonnie && isThought)
            {
                AddToClassList("dialogue-node-bonnie-thought");
            }
            else if (isBonnie)
            {
                AddToClassList("dialogue-node-bonnie");
            }
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

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

            speakerField = new TextField("Speaker");
            speakerField.multiline = false;
            speakerField.style.minWidth = 180;
            container.Add(speakerField);

            portraitField = new ObjectField("Portrait")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false
            };
            container.Add(portraitField);

            showPortraitToggle = new Toggle("Show Portrait Panel")
            {
                value = true,
                tooltip = "Controls whether the portrait panel is shown when this dialogue plays."
            };
            container.Add(showPortraitToggle);

            showSpeakerNameToggle = new Toggle("Show Speaker Name")
            {
                value = true,
                tooltip = "Controls whether the speaker name panel is shown when this dialogue plays."
            };
            container.Add(showSpeakerNameToggle);

            isThoughtToggle = new Toggle("Is Thought/Narration")
            {
                value = false,
                tooltip = "Displays dialogue in italics with optional thought bubble"
            };
            container.Add(isThoughtToggle);

            // Register callbacks to update node styling based on speaker and thought toggle
            speakerField.RegisterValueChangedCallback(evt => UpdateNodeStyling());
            isThoughtToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateNodeStyling();
                if (evt.newValue)
                {
                    showPortraitToggle.value = false;
                    showSpeakerNameToggle.value = false;
                }
            });

            requireInteractionToggle = new Toggle("Require Interaction")
            {
                value = true,
                tooltip = "If checked, player must click to advance. If unchecked, auto-advances after typewriter completes (useful for final dialogue before ending)"
            };
            container.Add(requireInteractionToggle);

            flipPortraitToggle = new Toggle("Flip Portrait")
            {
                value = false,
                tooltip = "Flip the portrait horizontally even when custom positioning is off."
            };
            container.Add(flipPortraitToggle);

            // Emotion swap section - checkbox, timing dropdown, and emotion dropdown on the same row
            var emotionSwapRow = new VisualElement();
            emotionSwapRow.style.flexDirection = FlexDirection.Row;
            emotionSwapRow.style.alignItems = Align.Center;
            emotionSwapRow.style.marginTop = 4;

            emotionSwapEnabledToggle = new Toggle("Emotion Swap")
            {
                value = false,
                tooltip = "Enable swapping to a different portrait based on emotion type (uses CharacterData portraits)"
            };
            emotionSwapRow.Add(emotionSwapEnabledToggle);

            emotionTypeField = new EnumField(EmotionType.Neutral);
            emotionTypeField.style.marginLeft = 8;
            emotionTypeField.style.minWidth = 80;
            emotionTypeField.tooltip = "Emotion type to swap to (matches portraitId in CharacterData)";
            emotionSwapRow.Add(emotionTypeField);

            emotionSwapTimingField = new EnumField(EmotionSwapTiming.Immediately);
            emotionSwapTimingField.style.marginLeft = 8;
            emotionSwapTimingField.style.minWidth = 100;
            emotionSwapTimingField.tooltip = "When to swap the portrait sprite";
            emotionSwapRow.Add(emotionSwapTimingField);

            container.Add(emotionSwapRow);

            // Update visibility based on toggle
            UpdateEmotionSwapFieldsState(emotionSwapEnabledToggle.value);
            emotionSwapEnabledToggle.RegisterValueChangedCallback(evt => UpdateEmotionSwapFieldsState(evt.newValue));

            // Portrait positioning section (collapsible)
            portraitFoldout = new Foldout { text = "Portrait Positioning", value = false };
            portraitFoldout.style.marginTop = 4;

            // Ending portrait front row (checkbox + index selector)
            var endingPortraitRow = new VisualElement();
            endingPortraitRow.style.flexDirection = FlexDirection.Row;
            endingPortraitRow.style.alignItems = Align.Center;

            useEndingPortraitFrontToggle = new Toggle("Ending Portrait Front")
            {
                value = false,
                tooltip = "Use the alternate PortraitImageFront GameObject instead of the default PortraitImage (for ending scene)"
            };
            endingPortraitRow.Add(useEndingPortraitFrontToggle);

            endingPortraitFrontIndexField = new SliderInt(1, 3)
            {
                value = 1,
                tooltip = "Which front portrait to use (1-3). Maps to PortraitImageFront-1, PortraitImageFront-2, PortraitImageFront-3"
            };
            endingPortraitFrontIndexField.style.marginLeft = 8;
            endingPortraitFrontIndexField.style.minWidth = 80;
            endingPortraitFrontIndexField.showInputField = true;
            endingPortraitFrontIndexField.SetEnabled(false);
            endingPortraitRow.Add(endingPortraitFrontIndexField);

            useEndingPortraitFrontToggle.RegisterValueChangedCallback(evt =>
            {
                endingPortraitFrontIndexField.SetEnabled(evt.newValue);
            });

            portraitFoldout.Add(endingPortraitRow);

            useCustomPositioningToggle = new Toggle("Use Custom Positioning")
            {
                value = false,
                tooltip = "Enable to customize portrait position, scale, and anchor. If disabled, uses default position from UI."
            };
            portraitFoldout.Add(useCustomPositioningToggle);

            portraitPositionField = new Vector2Field("Position")
            {
                tooltip = "Screen position in pixels (0,0 = center with default anchor)"
            };
            portraitPositionField.value = Vector2.zero;
            portraitFoldout.Add(portraitPositionField);

            portraitScaleField = new Slider("Scale", 0.1f, 3f)
            {
                value = 1f,
                tooltip = "Scale multiplier (1.0 = normal size)"
            };
            portraitScaleField.showInputField = true;
            portraitFoldout.Add(portraitScaleField);

            portraitAnchorField = new Vector2Field("Anchor")
            {
                tooltip = "Anchor point (0.5,0.5 = center, 0,0 = bottom-left, 1,1 = top-right)"
            };
            portraitAnchorField.value = new Vector2(0.5f, 0.5f);
            portraitFoldout.Add(portraitAnchorField);

            portraitSizeField = new Vector2Field("Size (Width/Height)")
            {
                tooltip = "Custom width and height for the portrait image in pixels"
            };
            portraitSizeField.value = new Vector2(2048f, 2048f);
            portraitFoldout.Add(portraitSizeField);

            flipPortraitField = new Toggle("Flip Horizontal")
            {
                value = false,
                tooltip = "Flip the portrait horizontally"
            };
            portraitFoldout.Add(flipPortraitField);
            flipPortraitField.RegisterValueChangedCallback(evt =>
            {
                if (isSyncingFlipPortrait) return;
                isSyncingFlipPortrait = true;
                flipPortraitToggle?.SetValueWithoutNotify(evt.newValue);
                isSyncingFlipPortrait = false;
            });
            flipPortraitToggle.RegisterValueChangedCallback(evt =>
            {
                if (isSyncingFlipPortrait) return;
                isSyncingFlipPortrait = true;
                flipPortraitField?.SetValueWithoutNotify(evt.newValue);
                isSyncingFlipPortrait = false;
            });

            swayEnabledToggle = new Toggle("Enable Sway")
            {
                value = false,
                tooltip = "Animate the portrait using a simple sway pattern"
            };
            portraitFoldout.Add(swayEnabledToggle);

            swayPatternField = new EnumField("Sway Pattern", SwayPattern.LeftRight);
            swayPatternField.tooltip = "Motion path for the sway animation";
            portraitFoldout.Add(swayPatternField);

            swaySpeedField = new Slider("Sway Speed", 0f, 10f)
            {
                value = 1f,
                tooltip = "Speed multiplier for the sway animation"
            };
            swaySpeedField.showInputField = true;
            portraitFoldout.Add(swaySpeedField);

            swayIntensityField = new Slider("Sway Intensity", 0f, 100f)
            {
                value = 10f,
                tooltip = "Movement distance (in pixels) for the sway animation"
            };
            swayIntensityField.showInputField = true;
            portraitFoldout.Add(swayIntensityField);

            swayEnabledToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateSwayFieldsState(evt.newValue);
                if (evt.newValue)
                {
                    TryInheritSwayFromPreviousNode();
                }
            });
            UpdateSwayFieldsState(swayEnabledToggle.value);

            sway2EnabledToggle = new Toggle("Enable Sway 2")
            {
                value = false,
                tooltip = "Second simultaneous sway channel (adds to Sway 1)"
            };
            portraitFoldout.Add(sway2EnabledToggle);

            sway2PatternField = new EnumField("Sway 2 Pattern", SwayPattern.UpDown);
            sway2PatternField.tooltip = "Motion path for the second sway channel";
            portraitFoldout.Add(sway2PatternField);

            sway2SpeedField = new Slider("Sway 2 Speed", 0f, 10f)
            {
                value = 1f,
                tooltip = "Speed multiplier for Sway 2"
            };
            sway2SpeedField.showInputField = true;
            portraitFoldout.Add(sway2SpeedField);

            sway2IntensityField = new Slider("Sway 2 Intensity", 0f, 100f)
            {
                value = 10f,
                tooltip = "Movement distance (pixels) for Sway 2"
            };
            sway2IntensityField.showInputField = true;
            portraitFoldout.Add(sway2IntensityField);

            sway2EnabledToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateSway2FieldsState(evt.newValue);
                if (evt.newValue)
                {
                    TryInheritSway2FromPreviousNode();
                }
            });
            UpdateSway2FieldsState(sway2EnabledToggle.value);

            rotationEnabledToggle = new Toggle("Enable Rotation")
            {
                value = false,
                tooltip = "Oscillate rotation between two angles"
            };
            portraitFoldout.Add(rotationEnabledToggle);

            rotationAxisField = new EnumField("Rotation Axis", RotationAxis.Z);
            rotationAxisField.tooltip = "Axis to rotate around";
            portraitFoldout.Add(rotationAxisField);

            rotationMinAngleField = new FloatField("Min Angle")
            {
                value = -5f,
                tooltip = "Minimum rotation angle (degrees)"
            };
            portraitFoldout.Add(rotationMinAngleField);

            rotationMaxAngleField = new FloatField("Max Angle")
            {
                value = 5f,
                tooltip = "Maximum rotation angle (degrees)"
            };
            portraitFoldout.Add(rotationMaxAngleField);

            rotationSpeedField = new Slider("Rotation Speed", 0f, 10f)
            {
                value = 1f,
                tooltip = "Speed multiplier for rotation oscillation"
            };
            rotationSpeedField.showInputField = true;
            portraitFoldout.Add(rotationSpeedField);

            rotationEnabledToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateRotationFieldsState(evt.newValue);
                if (evt.newValue)
                {
                    TryInheritRotationFromPreviousNode();
                }
            });
            UpdateRotationFieldsState(rotationEnabledToggle.value);

            scaleEnabledToggle = new Toggle("Enable Shrink/Grow")
            {
                value = false,
                tooltip = "Oscillate scale between min and max values"
            };
            portraitFoldout.Add(scaleEnabledToggle);

            scaleMinField = new Slider("Min Scale", 0.1f, 3f)
            {
                value = 1f,
                tooltip = "Minimum scale multiplier"
            };
            scaleMinField.showInputField = true;
            portraitFoldout.Add(scaleMinField);

            scaleMaxField = new Slider("Max Scale", 0.1f, 3f)
            {
                value = 1.1f,
                tooltip = "Maximum scale multiplier"
            };
            scaleMaxField.showInputField = true;
            portraitFoldout.Add(scaleMaxField);

            scaleSpeedField = new Slider("Scale Speed", 0f, 10f)
            {
                value = 1f,
                tooltip = "Speed multiplier for shrink/grow oscillation"
            };
            scaleSpeedField.showInputField = true;
            portraitFoldout.Add(scaleSpeedField);

            scaleEnabledToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateScaleFieldsState(evt.newValue);
                if (evt.newValue)
                {
                    TryInheritScaleFromPreviousNode();
                }
            });
            UpdateScaleFieldsState(scaleEnabledToggle.value);

            speakingScaleEnabledToggle = new Toggle("Speaking Scale Effect")
            {
                value = true,
                tooltip = "Subtle scale pulse while dialogue is typing to simulate speaking"
            };
            portraitFoldout.Add(speakingScaleEnabledToggle);

            // Enable/disable positioning fields based on toggle
            UpdatePositioningFieldsState(useCustomPositioningToggle.value);
            useCustomPositioningToggle.RegisterValueChangedCallback(evt =>
            {
                UpdatePositioningFieldsState(evt.newValue);
                if (evt.newValue)
                {
                    TryInheritCustomPositioningFromPreviousNode();
                }
            });

            container.Add(portraitFoldout);

            // Character spawn section (collapsible)
            characterSpawnFoldout = new Foldout { text = "Character Spawn", value = false };
            characterSpawnFoldout.style.marginTop = 4;

            characterPrefabField = new ObjectField("Character Prefab")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                tooltip = "Optional character prefab to spawn in the scene during this dialogue"
            };
            characterSpawnFoldout.Add(characterPrefabField);

            characterSpriteField = new ObjectField("Character Sprite")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                tooltip = "Full body sprite to pass to the character prefab"
            };
            characterSpawnFoldout.Add(characterSpriteField);

            characterSpawnPositionField = new Vector2Field("Spawn Position")
            {
                tooltip = "World position where character spawns"
            };
            characterSpawnPositionField.value = Vector2.zero;
            characterSpawnFoldout.Add(characterSpawnPositionField);

            characterScaleField = new Slider("Scale", 0.1f, 5f)
            {
                value = 1f,
                tooltip = "Scale multiplier for spawned character (1.0 = normal size)"
            };
            characterScaleField.showInputField = true;
            characterSpawnFoldout.Add(characterScaleField);

            flipCharacterField = new Toggle("Flip Horizontal")
            {
                value = false,
                tooltip = "Flip the spawned character horizontally"
            };
            characterSpawnFoldout.Add(flipCharacterField);

            persistCharacterField = new Toggle("Persist Character")
            {
                value = false,
                tooltip = "If checked, character stays until despawned. If unchecked, despawns when dialogue ends."
            };
            characterSpawnFoldout.Add(persistCharacterField);

            container.Add(characterSpawnFoldout);

            dialogueField = new TextField("Dialogue");
            dialogueField.multiline = true;
            dialogueField.style.minHeight = 60;
            dialogueField.style.whiteSpace = WhiteSpace.Normal;
            container.Add(dialogueField);

            // Audio section (collapsible)
            audioFoldout = new Foldout { text = "Audio", value = false };
            audioFoldout.style.marginTop = 4;

            voiceClipField = new ObjectField("Voice Clip")
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };
            audioFoldout.Add(voiceClipField);

            voiceDelayField = new FloatField("Voice Delay");
            voiceDelayField.value = 0f;
            audioFoldout.Add(voiceDelayField);

            ambientAudioField = new ObjectField("Ambient Audio")
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };
            ambientAudioField.style.marginTop = 4;
            audioFoldout.Add(ambientAudioField);

            loopAmbientField = new Toggle("Loop Ambient");
            audioFoldout.Add(loopAmbientField);

            container.Add(audioFoldout);

            extensionContainer.Add(container);

            // Add notes section
            CreateNotesSection();
        }

        private void UpdatePositioningFieldsState(bool enabled)
        {
            portraitPositionField?.SetEnabled(enabled);
            portraitScaleField?.SetEnabled(enabled);
            portraitAnchorField?.SetEnabled(enabled);
            portraitSizeField?.SetEnabled(enabled);
            flipPortraitField?.SetEnabled(enabled);
        }

        private void UpdateSwayFieldsState(bool enabled)
        {
            swayPatternField?.SetEnabled(enabled);
            swaySpeedField?.SetEnabled(enabled);
            swayIntensityField?.SetEnabled(enabled);
        }

        private void UpdateSway2FieldsState(bool enabled)
        {
            sway2PatternField?.SetEnabled(enabled);
            sway2SpeedField?.SetEnabled(enabled);
            sway2IntensityField?.SetEnabled(enabled);
        }

        private void UpdateRotationFieldsState(bool enabled)
        {
            rotationAxisField?.SetEnabled(enabled);
            rotationMinAngleField?.SetEnabled(enabled);
            rotationMaxAngleField?.SetEnabled(enabled);
            rotationSpeedField?.SetEnabled(enabled);
        }

        private void UpdateScaleFieldsState(bool enabled)
        {
            scaleMinField?.SetEnabled(enabled);
            scaleMaxField?.SetEnabled(enabled);
            scaleSpeedField?.SetEnabled(enabled);
        }

        private void UpdateEmotionSwapFieldsState(bool enabled)
        {
            emotionTypeField?.SetEnabled(enabled);
            emotionSwapTimingField?.SetEnabled(enabled);
        }

        private DialogueNode GetPreviousDialogueNode()
        {
            if (inputPort == null || !inputPort.connected)
                return null;

            foreach (var edge in inputPort.connections)
            {
                if (edge.output?.node is DialogueNode dialogueNode)
                {
                    return dialogueNode;
                }
            }
            return null;
        }

        private void TryInheritCustomPositioningFromPreviousNode()
        {
            var prevNode = GetPreviousDialogueNode();
            if (prevNode == null || prevNode.useCustomPositioningToggle?.value != true)
                return;

            portraitPositionField.value = prevNode.portraitPositionField?.value ?? Vector2.zero;
            portraitScaleField.value = prevNode.portraitScaleField?.value ?? 1f;
            portraitAnchorField.value = prevNode.portraitAnchorField?.value ?? new Vector2(0.5f, 0.5f);
            portraitSizeField.value = prevNode.portraitSizeField?.value ?? new Vector2(2048f, 2048f);
            ApplyFlipPortraitValue(prevNode.flipPortraitField?.value ?? false);
        }

        private void TryInheritSwayFromPreviousNode()
        {
            var prevNode = GetPreviousDialogueNode();
            if (prevNode == null || prevNode.swayEnabledToggle?.value != true)
                return;

            swayPatternField.value = prevNode.swayPatternField?.value ?? SwayPattern.LeftRight;
            swaySpeedField.value = prevNode.swaySpeedField?.value ?? 1f;
            swayIntensityField.value = prevNode.swayIntensityField?.value ?? 10f;
        }

        private void TryInheritSway2FromPreviousNode()
        {
            var prevNode = GetPreviousDialogueNode();
            if (prevNode == null || prevNode.sway2EnabledToggle?.value != true)
                return;

            sway2PatternField.value = prevNode.sway2PatternField?.value ?? SwayPattern.UpDown;
            sway2SpeedField.value = prevNode.sway2SpeedField?.value ?? 1f;
            sway2IntensityField.value = prevNode.sway2IntensityField?.value ?? 10f;
        }

        private void TryInheritRotationFromPreviousNode()
        {
            var prevNode = GetPreviousDialogueNode();
            if (prevNode == null || prevNode.rotationEnabledToggle?.value != true)
                return;

            rotationAxisField.value = prevNode.rotationAxisField?.value ?? RotationAxis.Z;
            rotationMinAngleField.value = prevNode.rotationMinAngleField?.value ?? -5f;
            rotationMaxAngleField.value = prevNode.rotationMaxAngleField?.value ?? 5f;
            rotationSpeedField.value = prevNode.rotationSpeedField?.value ?? 1f;
        }

        private void TryInheritScaleFromPreviousNode()
        {
            var prevNode = GetPreviousDialogueNode();
            if (prevNode == null || prevNode.scaleEnabledToggle?.value != true)
                return;

            scaleMinField.value = prevNode.scaleMinField?.value ?? 1f;
            scaleMaxField.value = prevNode.scaleMaxField?.value ?? 1.1f;
            scaleSpeedField.value = prevNode.scaleSpeedField?.value ?? 1f;
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var dialogueData = data as DialogueNodeData;
            if (dialogueData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            speakerField.value = dialogueData.speakerName ?? "";
            dialogueField.value = dialogueData.dialogueText ?? "";
            portraitField.value = dialogueData.portrait;
            if (showPortraitToggle != null)
            {
                showPortraitToggle.value = dialogueData.showPortraitPanel;
            }
            if (showSpeakerNameToggle != null)
            {
                showSpeakerNameToggle.SetValueWithoutNotify(dialogueData.showSpeakerName);
            }
            if (isThoughtToggle != null)
            {
                isThoughtToggle.SetValueWithoutNotify(dialogueData.isThought);
            }
            if (requireInteractionToggle != null)
            {
                requireInteractionToggle.SetValueWithoutNotify(dialogueData.requireInteraction);
            }

            // Load portrait positioning fields
            useEndingPortraitFrontToggle.value = dialogueData.useEndingPortraitFront;
            endingPortraitFrontIndexField.value = dialogueData.endingPortraitFrontIndex;
            endingPortraitFrontIndexField.SetEnabled(dialogueData.useEndingPortraitFront);
            useCustomPositioningToggle.value = dialogueData.useCustomPositioning;
            portraitPositionField.value = dialogueData.portraitPosition;
            portraitScaleField.value = dialogueData.portraitScale;
            portraitAnchorField.value = dialogueData.portraitAnchor;
            portraitSizeField.value = dialogueData.portraitSize;
            ApplyFlipPortraitValue(dialogueData.flipPortrait);

            // Load emotion swap fields
            emotionSwapEnabledToggle.value = dialogueData.emotionSwapEnabled;
            emotionTypeField.value = dialogueData.emotionType;
            emotionSwapTimingField.value = dialogueData.emotionSwapTiming;
            UpdateEmotionSwapFieldsState(dialogueData.emotionSwapEnabled);

            swayEnabledToggle.value = dialogueData.swayEnabled;
            swayPatternField.value = dialogueData.swayPattern;
            swaySpeedField.value = dialogueData.swaySpeed;
            swayIntensityField.value = dialogueData.swayIntensity;
            sway2EnabledToggle.value = dialogueData.sway2Enabled;
            sway2PatternField.value = dialogueData.sway2Pattern;
            sway2SpeedField.value = dialogueData.sway2Speed;
            sway2IntensityField.value = dialogueData.sway2Intensity;
            rotationEnabledToggle.value = dialogueData.rotationEnabled;
            rotationAxisField.value = dialogueData.rotationAxis;
            rotationMinAngleField.value = dialogueData.rotationMinAngle;
            rotationMaxAngleField.value = dialogueData.rotationMaxAngle;
            rotationSpeedField.value = dialogueData.rotationSpeed;
            scaleEnabledToggle.value = dialogueData.scaleEnabled;
            scaleMinField.value = dialogueData.scaleMin;
            scaleMaxField.value = dialogueData.scaleMax;
            scaleSpeedField.value = dialogueData.scaleSpeed;
            speakingScaleEnabledToggle.value = dialogueData.speakingScaleEnabled;

            // Update field enabled state
            UpdatePositioningFieldsState(dialogueData.useCustomPositioning);
            UpdateSwayFieldsState(dialogueData.swayEnabled);
            UpdateSway2FieldsState(dialogueData.sway2Enabled);
            UpdateRotationFieldsState(dialogueData.rotationEnabled);
            UpdateScaleFieldsState(dialogueData.scaleEnabled);

            // Expand portrait foldout if custom positioning is enabled
            if (dialogueData.useCustomPositioning || dialogueData.swayEnabled || dialogueData.sway2Enabled || dialogueData.rotationEnabled || dialogueData.scaleEnabled)
            {
                portraitFoldout.value = true;
            }

            // Load character spawn fields
            characterPrefabField.value = dialogueData.characterPrefab;
            characterSpriteField.value = dialogueData.characterSprite;
            characterSpawnPositionField.value = dialogueData.characterSpawnPosition;
            characterScaleField.value = dialogueData.characterScale;
            flipCharacterField.value = dialogueData.flipCharacter;
            persistCharacterField.value = dialogueData.persistCharacter;

            // Expand character spawn foldout if prefab is set
            if (dialogueData.characterPrefab != null)
            {
                characterSpawnFoldout.value = true;
            }

            // Load audio fields
            voiceClipField.value = dialogueData.voiceClip;
            ambientAudioField.value = dialogueData.ambientAudio;
            loopAmbientField.value = dialogueData.loopAmbient;
            voiceDelayField.value = dialogueData.voiceDelay;

            // Expand audio foldout if any audio is set
            if (dialogueData.voiceClip != null || dialogueData.ambientAudio != null)
            {
                audioFoldout.value = true;
            }

            // Load notes and expanded state
            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            // Update node styling based on speaker and thought toggle
            UpdateNodeStyling();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void ApplyFlipPortraitValue(bool value)
        {
            isSyncingFlipPortrait = true;
            flipPortraitField?.SetValueWithoutNotify(value);
            flipPortraitToggle?.SetValueWithoutNotify(value);
            isSyncingFlipPortrait = false;
        }

        public override BaseNodeData SaveToData()
        {
            var data = new DialogueNodeData
            {
                guid = Guid,
                nodeType = "Dialogue",
                position = GetPosition().position,
                speakerName = SpeakerName,
                dialogueText = DialogueText,
                portrait = Portrait,
                showPortraitPanel = showPortraitToggle?.value ?? true,
                showSpeakerName = showSpeakerNameToggle?.value ?? true,
                isThought = isThoughtToggle?.value ?? false,
                requireInteraction = requireInteractionToggle?.value ?? true,
                useEndingPortraitFront = useEndingPortraitFrontToggle?.value ?? false,
                endingPortraitFrontIndex = endingPortraitFrontIndexField?.value ?? 1,
                useCustomPositioning = useCustomPositioningToggle?.value ?? false,
                portraitPosition = portraitPositionField?.value ?? Vector2.zero,
                portraitScale = portraitScaleField?.value ?? 1f,
                portraitAnchor = portraitAnchorField?.value ?? new Vector2(0.5f, 0.5f),
                portraitSize = portraitSizeField?.value ?? new Vector2(2048f, 2048f),
                flipPortrait = flipPortraitToggle?.value ?? false,
                emotionSwapEnabled = emotionSwapEnabledToggle?.value ?? false,
                emotionType = emotionTypeField?.value is EmotionType etype ? etype : EmotionType.Neutral,
                emotionSwapTiming = emotionSwapTimingField?.value is EmotionSwapTiming timing ? timing : EmotionSwapTiming.Immediately,
                swayEnabled = swayEnabledToggle?.value ?? false,
                swayPattern = swayPatternField?.value is SwayPattern pattern ? pattern : SwayPattern.LeftRight,
                swaySpeed = swaySpeedField?.value ?? 1f,
                swayIntensity = swayIntensityField?.value ?? 10f,
                sway2Enabled = sway2EnabledToggle?.value ?? false,
                sway2Pattern = sway2PatternField?.value is SwayPattern pattern2 ? pattern2 : SwayPattern.UpDown,
                sway2Speed = sway2SpeedField?.value ?? 1f,
                sway2Intensity = sway2IntensityField?.value ?? 10f,
                rotationEnabled = rotationEnabledToggle?.value ?? false,
                rotationAxis = rotationAxisField?.value is RotationAxis axis ? axis : RotationAxis.Z,
                rotationMinAngle = rotationMinAngleField?.value ?? -5f,
                rotationMaxAngle = rotationMaxAngleField?.value ?? 5f,
                rotationSpeed = rotationSpeedField?.value ?? 1f,
                scaleEnabled = scaleEnabledToggle?.value ?? false,
                scaleMin = scaleMinField?.value ?? 1f,
                scaleMax = scaleMaxField?.value ?? 1.1f,
                scaleSpeed = scaleSpeedField?.value ?? 1f,
                speakingScaleEnabled = speakingScaleEnabledToggle?.value ?? true,
                characterPrefab = characterPrefabField?.value as GameObject,
                characterSprite = characterSpriteField?.value as Sprite,
                characterSpawnPosition = characterSpawnPositionField?.value ?? Vector2.zero,
                characterScale = characterScaleField?.value ?? 1f,
                flipCharacter = flipCharacterField?.value ?? false,
                persistCharacter = persistCharacterField?.value ?? false,
                voiceClip = VoiceClip,
                ambientAudio = AmbientAudio,
                loopAmbient = LoopAmbient,
                voiceDelay = VoiceDelay
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
