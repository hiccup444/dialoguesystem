using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class StopSoundNode : BaseDialogueNode
    {
        private EnumField channelTypeField;
        private FloatField fadeOutDurationField;
        private Toggle waitForCompletionToggle;

        public AudioChannelType ChannelType => channelTypeField?.value is AudioChannelType ct ? ct : AudioChannelType.Music;
        public float FadeOutDuration => fadeOutDurationField?.value ?? 0f;
        public bool WaitForCompletion => waitForCompletionToggle?.value ?? false;

        public StopSoundNode()
        {
            title = "Stop Sound";
            AddToClassList("stop-sound-node");
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
            container.style.minWidth = 200;

            // Channel type field
            channelTypeField = new EnumField("Channel", AudioChannelType.Music)
            {
                tooltip = "Which audio channel to stop (Music, SFX, or Voice)"
            };
            container.Add(channelTypeField);

            // Fade out duration field
            fadeOutDurationField = new FloatField("Fade Out (s)")
            {
                value = 0f,
                tooltip = "Duration of the fade out before stopping (0 for instant stop)"
            };
            fadeOutDurationField.style.marginTop = 4;
            container.Add(fadeOutDurationField);

            // Wait for completion toggle
            waitForCompletionToggle = new Toggle("Wait For Completion")
            {
                value = false,
                tooltip = "If enabled, waits for the fade out to complete before continuing to the next node"
            };
            waitForCompletionToggle.style.marginTop = 4;
            container.Add(waitForCompletionToggle);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var stopSoundData = data as StopSoundNodeData;
            if (stopSoundData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            channelTypeField.value = stopSoundData.channelType;
            fadeOutDurationField.value = stopSoundData.fadeOutDuration;
            waitForCompletionToggle.value = stopSoundData.waitForCompletion;

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new StopSoundNodeData
            {
                guid = Guid,
                nodeType = "StopSound",
                position = GetPosition().position,
                channelType = ChannelType,
                fadeOutDuration = FadeOutDuration,
                waitForCompletion = WaitForCompletion
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
