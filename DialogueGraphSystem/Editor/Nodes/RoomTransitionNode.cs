using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class RoomTransitionNode : BaseDialogueNode
    {
        private TextField sceneNameField;
        private Toggle waitForCompletionField;
        private FloatField postDelayField;

        public string TargetSceneName => sceneNameField?.value ?? string.Empty;
        public bool WaitForCompletion => waitForCompletionField?.value ?? true;
        public float PostTransitionDelay => postDelayField?.value ?? 0f;

        public RoomTransitionNode()
        {
            title = "Room Transition";
            AddToClassList("room-transition-node");
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
            container.style.minWidth = 220;

            sceneNameField = new TextField("Target Scene Name")
            {
                tooltip = "Scene name or identifier to load when this node runs. Subscribe to DialogueManager.OnRoomTransitionExecuted to handle loading."
            };
            container.Add(sceneNameField);

            waitForCompletionField = new Toggle("Wait For Transition")
            {
                value = true,
                tooltip = "Hold dialogue execution until IsRoomTransitioning delegate returns false"
            };
            container.Add(waitForCompletionField);

            postDelayField = new FloatField("Post-Transition Delay (s)")
            {
                value = 0f,
                tooltip = "Additional delay after the transition completes before continuing"
            };
            container.Add(postDelayField);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var roomData = data as RoomTransitionNodeData;
            if (roomData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            sceneNameField.value = roomData.targetSceneName;
            waitForCompletionField.value = roomData.waitForTransitionCompletion;
            postDelayField.value = roomData.postTransitionDelay;

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new RoomTransitionNodeData
            {
                guid = Guid,
                nodeType = "RoomTransition",
                position = GetPosition().position,
                targetSceneName = TargetSceneName,
                waitForTransitionCompletion = WaitForCompletion,
                postTransitionDelay = PostTransitionDelay
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
