using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class NotLastCharacterNode : BaseDialogueNode
    {
        public NotLastCharacterNode()
        {
            title = "Not Last Character";
            AddToClassList("not-last-character-node");
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
            container.style.minWidth = 180;

            var label = new Label("Prevents automatic last_character\ntracking for this dialogue");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new Color(0.7f, 0.7f, 0.7f);
            label.style.fontSize = 10;
            container.Add(label);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var nodeData = data as NotLastCharacterNodeData;
            if (nodeData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new NotLastCharacterNodeData
            {
                guid = Guid,
                nodeType = "NotLastCharacter",
                position = GetPosition().position
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
