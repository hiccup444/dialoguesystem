using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class GraphTransitionNode : BaseDialogueNode
    {
        private ObjectField graphField;

        public DialogueGraphAsset TargetGraph => graphField?.value as DialogueGraphAsset;

        public GraphTransitionNode()
        {
            title = "Graph Transition";
            AddToClassList("graph-transition-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");

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

            graphField = new ObjectField("Target Graph")
            {
                objectType = typeof(DialogueGraphAsset),
                allowSceneObjects = false,
                tooltip = "Dialogue graph to start when this node executes"
            };

            container.Add(graphField);

            extensionContainer.Add(container);
            CreateNotesSection();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var transitionData = data as GraphTransitionNodeData;
            if (transitionData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");

            CreateFields();

            graphField.value = transitionData.targetGraph;

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new GraphTransitionNodeData
            {
                guid = Guid,
                nodeType = "GraphTransition",
                position = GetPosition().position,
                targetGraph = TargetGraph
            };

            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
