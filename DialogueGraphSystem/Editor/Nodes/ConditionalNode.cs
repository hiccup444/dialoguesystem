using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class ConditionalNode : BaseDialogueNode
    {
        private EnumField variableTypeField;
        private TextField variableNameField;
        private EnumField comparisonField;
        private IntegerField compareValueField;
        private Toggle expectedBoolField;

        private VisualElement intContainer;
        private VisualElement boolContainer;

        private Port truePort;
        private Port falsePort;

        private string truePortGuid;
        private string falsePortGuid;

        public VariableType VariableType => (VariableType)(variableTypeField?.value ?? VariableType.Bool);
        public string VariableName => variableNameField?.value ?? "";
        public ComparisonOperator Comparison => (ComparisonOperator)(comparisonField?.value ?? ComparisonOperator.Equal);
        public int CompareValue => compareValueField?.value ?? 0;
        public bool ExpectedBoolValue => expectedBoolField?.value ?? true;

        public ConditionalNode()
        {
            title = "Conditional";
            AddToClassList("conditional-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            truePortGuid = System.Guid.NewGuid().ToString();
            falsePortGuid = System.Guid.NewGuid().ToString();

            inputPort = CreateInputPort("Input");
            CreateOutputPorts();
            CreateFields();
            UpdateFieldVisibility();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreateOutputPorts()
        {
            truePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            truePort.portName = "True";
            truePort.portColor = new Color(0.3f, 0.8f, 0.3f);
            outputContainer.Add(truePort);

            falsePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            falsePort.portName = "False";
            falsePort.portColor = new Color(0.8f, 0.3f, 0.3f);
            outputContainer.Add(falsePort);
        }

        private void CreateFields()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.minWidth = 180;

            variableTypeField = new EnumField("Type", VariableType.Bool);
            variableTypeField.RegisterValueChangedCallback(evt => UpdateFieldVisibility());
            container.Add(variableTypeField);

            variableNameField = new TextField("Variable");
            variableNameField.style.marginTop = 4;
            container.Add(variableNameField);

            // Int comparison container
            intContainer = new VisualElement();
            intContainer.style.marginTop = 4;

            comparisonField = new EnumField("Comparison", ComparisonOperator.Equal);
            intContainer.Add(comparisonField);

            compareValueField = new IntegerField("Value");
            compareValueField.style.marginTop = 2;
            intContainer.Add(compareValueField);

            container.Add(intContainer);

            // Bool comparison container
            boolContainer = new VisualElement();
            boolContainer.style.marginTop = 4;

            expectedBoolField = new Toggle("Expected Value");
            expectedBoolField.value = true;
            boolContainer.Add(expectedBoolField);

            container.Add(boolContainer);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        private void UpdateFieldVisibility()
        {
            var isInt = VariableType == VariableType.Int;
            intContainer.style.display = isInt ? DisplayStyle.Flex : DisplayStyle.None;
            boolContainer.style.display = isInt ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var conditionalData = data as ConditionalNodeData;
            if (conditionalData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            truePortGuid = conditionalData.truePortGuid ?? System.Guid.NewGuid().ToString();
            falsePortGuid = conditionalData.falsePortGuid ?? System.Guid.NewGuid().ToString();

            inputPort = CreateInputPort("Input");
            CreateOutputPorts();
            CreateFields();

            variableTypeField.value = conditionalData.variableType;
            variableNameField.value = conditionalData.variableName ?? "";
            comparisonField.value = conditionalData.comparison;
            compareValueField.value = conditionalData.compareValue;
            expectedBoolField.value = conditionalData.expectedBoolValue;

            UpdateFieldVisibility();

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new ConditionalNodeData
            {
                guid = Guid,
                nodeType = "Conditional",
                position = GetPosition().position,
                variableType = VariableType,
                variableName = VariableName,
                comparison = Comparison,
                compareValue = CompareValue,
                expectedBoolValue = ExpectedBoolValue,
                truePortGuid = truePortGuid,
                falsePortGuid = falsePortGuid
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }

        public Port GetTruePort() => truePort;
        public Port GetFalsePort() => falsePort;
        public string GetTruePortGuid() => truePortGuid;
        public string GetFalsePortGuid() => falsePortGuid;

        public Port GetOutputPortByGuid(string guid)
        {
            if (guid == truePortGuid) return truePort;
            if (guid == falsePortGuid) return falsePort;
            return null;
        }
    }
}
