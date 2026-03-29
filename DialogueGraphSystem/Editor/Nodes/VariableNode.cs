using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class VariableNode : BaseDialogueNode
    {
        private EnumField variableTypeField;
        private TextField variableNameField;
        private EnumField intOperationField;
        private IntegerField intValueField;
        private EnumField boolOperationField;
        private Toggle boolValueField;

        private VisualElement intContainer;
        private VisualElement boolContainer;

        public VariableType VariableType => (VariableType)(variableTypeField?.value ?? VariableType.Int);
        public string VariableName => variableNameField?.value ?? "";
        public IntOperation IntOperation => (IntOperation)(intOperationField?.value ?? IntOperation.Set);
        public int IntValue => intValueField?.value ?? 0;
        public BoolOperation BoolOperation => (BoolOperation)(boolOperationField?.value ?? BoolOperation.Set);
        public bool BoolValue => boolValueField?.value ?? false;

        public VariableNode()
        {
            title = "Variable";
            AddToClassList("variable-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();
            UpdateFieldVisibility();

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

            variableTypeField = new EnumField("Type", VariableType.Int);
            variableTypeField.RegisterValueChangedCallback(evt => UpdateFieldVisibility());
            container.Add(variableTypeField);

            variableNameField = new TextField("Variable");
            variableNameField.style.marginTop = 4;
            container.Add(variableNameField);

            // Int operation container
            intContainer = new VisualElement();
            intContainer.style.marginTop = 4;

            intOperationField = new EnumField("Operation", IntOperation.Set);
            intContainer.Add(intOperationField);

            intValueField = new IntegerField("Value");
            intValueField.style.marginTop = 2;
            intContainer.Add(intValueField);

            container.Add(intContainer);

            // Bool operation container
            boolContainer = new VisualElement();
            boolContainer.style.marginTop = 4;

            boolOperationField = new EnumField("Operation", BoolOperation.Set);
            boolOperationField.RegisterValueChangedCallback(evt => UpdateBoolValueVisibility());
            boolContainer.Add(boolOperationField);

            boolValueField = new Toggle("Value");
            boolValueField.style.marginTop = 2;
            boolContainer.Add(boolValueField);

            container.Add(boolContainer);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        private void UpdateFieldVisibility()
        {
            var isInt = VariableType == VariableType.Int;
            intContainer.style.display = isInt ? DisplayStyle.Flex : DisplayStyle.None;
            boolContainer.style.display = isInt ? DisplayStyle.None : DisplayStyle.Flex;

            if (!isInt)
            {
                UpdateBoolValueVisibility();
            }
        }

        private void UpdateBoolValueVisibility()
        {
            // Hide value field when Toggle operation is selected
            var isToggle = BoolOperation == BoolOperation.Toggle;
            boolValueField.style.display = isToggle ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var variableData = data as VariableNodeData;
            if (variableData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            variableTypeField.value = variableData.variableType;
            variableNameField.value = variableData.variableName ?? "";
            intOperationField.value = variableData.intOperation;
            intValueField.value = variableData.intValue;
            boolOperationField.value = variableData.boolOperation;
            boolValueField.value = variableData.boolValue;

            UpdateFieldVisibility();

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new VariableNodeData
            {
                guid = Guid,
                nodeType = "Variable",
                position = GetPosition().position,
                variableType = VariableType,
                variableName = VariableName,
                intOperation = IntOperation,
                intValue = IntValue,
                boolOperation = BoolOperation,
                boolValue = BoolValue
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
