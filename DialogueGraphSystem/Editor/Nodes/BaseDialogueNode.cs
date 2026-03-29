using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;
using VNWinter.DialogueGraph.Editor;

namespace VNWinter.DialogueGraph.Editor
{
    public abstract class BaseDialogueNode : Node
    {
        public string Guid { get; protected set; }

        protected Port inputPort;
        protected Port outputPort;

        protected Foldout notesContainer;
        protected TextField notesField;

        public virtual void Initialize(Vector2 position)
        {
            Guid = System.Guid.NewGuid().ToString();
            SetPosition(new Rect(position, Vector2.zero));

            // Allow resize by default
            capabilities |= Capabilities.Resizable;
            // Provide reasonable minimums so content stays visible
            style.minWidth = 220f;
            style.minHeight = 140f;

            // Add a resize handle
            var resizer = new GraphElementResizer(220f, 140f);
            hierarchy.Add(resizer);
        }

        public abstract void LoadFromData(BaseNodeData data);
        public abstract BaseNodeData SaveToData();

        protected Port CreateInputPort(string name = "Input", Port.Capacity capacity = Port.Capacity.Multi)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, typeof(float));
            port.portName = name;
            inputContainer.Add(port);
            return port;
        }

        protected Port CreateOutputPort(string name = "Output", Port.Capacity capacity = Port.Capacity.Single)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(float));
            port.portName = name;
            outputContainer.Add(port);
            return port;
        }

        protected void CreateNotesSection()
        {
            notesContainer = new Foldout { text = "Designer Notes", value = false };
            notesContainer.style.marginTop = 4;
            notesField = new TextField { multiline = true };
            notesField.style.minHeight = 40;
            notesField.style.whiteSpace = WhiteSpace.Normal;
            notesContainer.Add(notesField);
            extensionContainer.Add(notesContainer);
            RefreshExpandedState();
        }

        protected void LoadNotesFromData(BaseNodeData data)
        {
            if (notesField != null && data != null)
            {
                notesField.value = data.designerNotes ?? string.Empty;
                notesContainer.value = !string.IsNullOrEmpty(data.designerNotes);
            }
        }

        protected void LoadExpandedStateFromData(BaseNodeData data)
        {
            if (data != null)
            {
                expanded = data.isExpanded;
            }
        }

        protected void SaveNotesToData(BaseNodeData data)
        {
            if (notesField != null && data != null)
            {
                data.designerNotes = notesField.value;
            }
        }

        protected void SaveExpandedStateToData(BaseNodeData data)
        {
            if (data != null)
            {
                data.isExpanded = expanded;
            }
        }

        public Port GetInputPort()
        {
            return inputPort;
        }

        public Port GetOutputPort()
        {
            return outputPort;
        }
    }
}
