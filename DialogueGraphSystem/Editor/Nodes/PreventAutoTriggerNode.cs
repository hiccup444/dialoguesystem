using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class PreventAutoTriggerNode : BaseDialogueNode
    {
        public PreventAutoTriggerNode()
        {
            title = "Prevent Auto Trigger";
            AddToClassList("prevent-auto-trigger-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            // Add info label
            var infoLabel = new Label("Blocks auto-triggered dialogue.\nAllows manual interactions.");
            infoLabel.style.fontSize = 11;
            infoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            infoLabel.style.marginTop = 4;
            infoLabel.style.marginBottom = 4;
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            mainContainer.Add(infoLabel);

            CreateNotesSection();

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            // Add info label
            var infoLabel = new Label("Blocks auto-triggered dialogue.\nAllows manual interactions.");
            infoLabel.style.fontSize = 11;
            infoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            infoLabel.style.marginTop = 4;
            infoLabel.style.marginBottom = 4;
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            mainContainer.Add(infoLabel);

            CreateNotesSection();
            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new PreventAutoTriggerNodeData
            {
                guid = Guid,
                nodeType = "PreventAutoTrigger",
                position = GetPosition().position
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
