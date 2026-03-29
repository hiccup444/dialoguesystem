using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class StartNode : BaseDialogueNode
    {
        public StartNode()
        {
            title = "Start";
            AddToClassList("start-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            outputPort = CreateOutputPort("Start");

            CreateNotesSection();

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            outputPort = CreateOutputPort("Start");

            CreateNotesSection();
            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new StartNodeData
            {
                guid = Guid,
                nodeType = "Start",
                position = GetPosition().position
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
