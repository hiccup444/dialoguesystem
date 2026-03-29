using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class AudioNode : BaseDialogueNode
    {
        private EnumField channelTypeField;
        private ObjectField audioClipField;
        private Toggle loopToggle;
        private Slider targetVolumeField;
        private FloatField fadeDurationField;
        private Toggle waitForCompletionToggle;

        public AudioChannelType ChannelType => channelTypeField?.value is AudioChannelType ct ? ct : AudioChannelType.Music;
        public AudioClip AudioClip => audioClipField?.value as AudioClip;
        public bool Loop => loopToggle?.value ?? true;
        public float TargetVolume => targetVolumeField?.value ?? 1f;
        public float FadeDuration => fadeDurationField?.value ?? 1f;
        public bool WaitForCompletion => waitForCompletionToggle?.value ?? false;

        public AudioNode()
        {
            title = "Audio";
            AddToClassList("audio-node");
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
            container.style.minWidth = 250;

            // Channel type field
            channelTypeField = new EnumField("Channel", AudioChannelType.Music)
            {
                tooltip = "Which audio channel to play on or modify (Music, SFX, or Voice)"
            };
            container.Add(channelTypeField);

            // Audio clip field
            audioClipField = new ObjectField("Audio Clip")
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false,
                tooltip = "Audio clip to play. Leave empty to only adjust volume of currently playing track."
            };
            audioClipField.style.marginTop = 4;
            container.Add(audioClipField);

            // Loop toggle
            loopToggle = new Toggle("Loop")
            {
                value = true,
                tooltip = "Whether the audio should loop (typically true for music, false for SFX)"
            };
            loopToggle.style.marginTop = 4;
            container.Add(loopToggle);

            // Target volume slider
            targetVolumeField = new Slider("Volume", 0f, 1f)
            {
                value = 1f,
                tooltip = "Target volume level (0 = silent, 1 = full volume)"
            };
            targetVolumeField.showInputField = true;
            targetVolumeField.style.marginTop = 4;
            container.Add(targetVolumeField);

            // Fade duration field
            fadeDurationField = new FloatField("Fade Duration (s)")
            {
                value = 1f,
                tooltip = "Duration of the volume fade in seconds (0 for instant)"
            };
            fadeDurationField.style.marginTop = 4;
            container.Add(fadeDurationField);

            // Wait for completion toggle
            waitForCompletionToggle = new Toggle("Wait For Completion")
            {
                value = false,
                tooltip = "If enabled, waits for the fade to complete before continuing to the next node"
            };
            waitForCompletionToggle.style.marginTop = 4;
            container.Add(waitForCompletionToggle);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var audioData = data as AudioNodeData;
            if (audioData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            channelTypeField.value = audioData.channelType;
            audioClipField.value = audioData.audioClip;
            loopToggle.value = audioData.loop;
            targetVolumeField.value = audioData.targetVolume;
            fadeDurationField.value = audioData.fadeDuration;
            waitForCompletionToggle.value = audioData.waitForCompletion;

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new AudioNodeData
            {
                guid = Guid,
                nodeType = "Audio",
                position = GetPosition().position,
                channelType = ChannelType,
                audioClip = AudioClip,
                loop = Loop,
                targetVolume = TargetVolume,
                fadeDuration = FadeDuration,
                waitForCompletion = WaitForCompletion
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
