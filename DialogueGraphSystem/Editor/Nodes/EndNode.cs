using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class EndNode : BaseDialogueNode
    {
        public EndNode()
        {
            title = "End";
            AddToClassList("end-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");

            CreateNotesSection();

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");

            CreateNotesSection();
            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new EndNodeData
            {
                guid = Guid,
                nodeType = "End",
                position = GetPosition().position
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
