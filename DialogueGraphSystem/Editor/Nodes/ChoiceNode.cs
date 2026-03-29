using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class ChoiceNode : BaseDialogueNode
    {
        private List<ChoicePortData> choicePorts = new List<ChoicePortData>();
        private VisualElement choicesContainer;

        public ChoiceNode()
        {
            title = "Choice";
            AddToClassList("choice-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");

            CreateChoicesContainer();
            AddChoice();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreateChoicesContainer()
        {
            choicesContainer = new VisualElement();
            choicesContainer.style.paddingLeft = 4;
            choicesContainer.style.paddingRight = 4;
            choicesContainer.style.paddingTop = 4;
            choicesContainer.style.paddingBottom = 4;

            var addButton = new Button(() => AddChoice()) { text = "+ Add Choice" };
            addButton.style.marginTop = 4;

            extensionContainer.Add(choicesContainer);
            extensionContainer.Add(addButton);

            CreateNotesSection();
        }

        private void AddChoice(string text = "", string guid = null, int conversationPoints = 0)
        {
            var choiceGuid = guid ?? System.Guid.NewGuid().ToString();
            int choiceNumber = choicePorts.Count + 1;

            var choiceRow = new VisualElement();
            choiceRow.style.flexDirection = FlexDirection.Row;
            choiceRow.style.marginBottom = 2;

            var label = new Label($"Choice {choiceNumber}");
            label.style.minWidth = 55;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            choiceRow.Add(label);

            var textField = new TextField() { value = text };
            textField.style.flexGrow = 1;
            textField.style.minWidth = 100;
            choiceRow.Add(textField);

            var pointsLabel = new Label("Pts");
            pointsLabel.style.marginLeft = 4;
            pointsLabel.style.minWidth = 24;
            pointsLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            choiceRow.Add(pointsLabel);

            var pointsField = new IntegerField();
            pointsField.value = conversationPoints;
            pointsField.labelElement.style.display = DisplayStyle.None;
            pointsField.style.width = 60;
            pointsField.style.minWidth = 60;
            pointsField.style.marginLeft = 2;
            choiceRow.Add(pointsField);

            var removeBtn = new Button(() => RemoveChoice(choiceGuid)) { text = "X" };
            removeBtn.style.width = 24;
            removeBtn.style.marginLeft = 4;
            choiceRow.Add(removeBtn);

            choicesContainer.Add(choiceRow);

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            port.portName = $"Choice {choiceNumber}";
            outputContainer.Add(port);

            choicePorts.Add(new ChoicePortData
            {
                guid = choiceGuid,
                textField = textField,
                pointsField = pointsField,
                port = port,
                row = choiceRow,
                label = label,
                index = choiceNumber
            });

            RefreshExpandedState();
            RefreshPorts();
        }

        private void RemoveChoice(string guid)
        {
            if (choicePorts.Count <= 1) return;

            var choiceData = choicePorts.Find(c => c.guid == guid);
            if (choiceData != null)
            {
                var edges = choiceData.port.connections.ToList();
                foreach (var edge in edges)
                {
                    edge.input?.Disconnect(edge);
                    edge.output?.Disconnect(edge);
                    edge.RemoveFromHierarchy();
                }

                choicesContainer.Remove(choiceData.row);
                outputContainer.Remove(choiceData.port);
                choicePorts.Remove(choiceData);

                UpdateChoiceNumbers();

                RefreshExpandedState();
                RefreshPorts();
            }
        }

        private void UpdateChoiceNumbers()
        {
            for (int i = 0; i < choicePorts.Count; i++)
            {
                int choiceNumber = i + 1;
                choicePorts[i].index = choiceNumber;
                choicePorts[i].label.text = $"Choice {choiceNumber}";
                choicePorts[i].port.portName = $"Choice {choiceNumber}";
            }
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var choiceData = data as ChoiceNodeData;
            if (choiceData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");

            CreateChoicesContainer();

            if (choiceData.choices != null && choiceData.choices.Count > 0)
            {
                foreach (var choice in choiceData.choices)
                {
                    AddChoice(choice.choiceText, choice.choiceGuid, choice.conversationPoints);
                }
            }
            else
            {
                AddChoice();
            }

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new ChoiceNodeData
            {
                guid = Guid,
                nodeType = "Choice",
                position = GetPosition().position,
                choices = choicePorts.Select(cp => new ChoiceData
                {
                    choiceGuid = cp.guid,
                    choiceText = cp.textField.value,
                    conversationPoints = cp.pointsField?.value ?? 0
                }).ToList()
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }

        public Port GetOutputPortByGuid(string guid)
        {
            var choice = choicePorts.Find(c => c.guid == guid);
            return choice?.port;
        }

        public Port GetOutputPortByIndex(int index)
        {
            if (index >= 0 && index < choicePorts.Count)
            {
                return choicePorts[index].port;
            }
            return null;
        }

        public string GetGuidByIndex(int index)
        {
            if (index >= 0 && index < choicePorts.Count)
            {
                return choicePorts[index].guid;
            }
            return null;
        }

        private class ChoicePortData
        {
            public string guid;
            public TextField textField;
            public IntegerField pointsField;
            public Port port;
            public VisualElement row;
            public Label label;
            public int index;
        }
    }
}
