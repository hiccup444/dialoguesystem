using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class RandomNode : BaseDialogueNode
    {
        private List<Port> outputPorts = new List<Port>();
        private string output1PortGuid;
        private string output2PortGuid;
        private string output3PortGuid;
        private string output4PortGuid;

        public RandomNode()
        {
            title = "Random";
            AddToClassList("random-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");

            // Generate GUIDs for the 4 output ports
            output1PortGuid = System.Guid.NewGuid().ToString();
            output2PortGuid = System.Guid.NewGuid().ToString();
            output3PortGuid = System.Guid.NewGuid().ToString();
            output4PortGuid = System.Guid.NewGuid().ToString();

            // Create 4 output ports
            for (int i = 1; i <= 4; i++)
            {
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
                port.portName = $"Output {i}";
                outputContainer.Add(port);
                outputPorts.Add(port);
            }

            CreateNotesSection();

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var randomData = data as RandomNodeData;
            if (randomData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            // Load port GUIDs or generate new ones if not present
            output1PortGuid = randomData.output1PortGuid ?? System.Guid.NewGuid().ToString();
            output2PortGuid = randomData.output2PortGuid ?? System.Guid.NewGuid().ToString();
            output3PortGuid = randomData.output3PortGuid ?? System.Guid.NewGuid().ToString();
            output4PortGuid = randomData.output4PortGuid ?? System.Guid.NewGuid().ToString();

            inputPort = CreateInputPort("Input");

            // Create 4 output ports
            for (int i = 1; i <= 4; i++)
            {
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
                port.portName = $"Output {i}";
                outputContainer.Add(port);
                outputPorts.Add(port);
            }

            CreateNotesSection();
            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new RandomNodeData
            {
                guid = Guid,
                nodeType = "Random",
                position = GetPosition().position,
                output1PortGuid = output1PortGuid,
                output2PortGuid = output2PortGuid,
                output3PortGuid = output3PortGuid,
                output4PortGuid = output4PortGuid
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }

        public Port GetOutputPort(int index)
        {
            if (index >= 0 && index < outputPorts.Count)
                return outputPorts[index];
            return null;
        }

        public string GetPortGuid(int index)
        {
            return index switch
            {
                0 => output1PortGuid,
                1 => output2PortGuid,
                2 => output3PortGuid,
                3 => output4PortGuid,
                _ => null
            };
        }

        public Port GetOutputPortByGuid(string guid)
        {
            if (guid == output1PortGuid && outputPorts.Count > 0)
                return outputPorts[0];
            if (guid == output2PortGuid && outputPorts.Count > 1)
                return outputPorts[1];
            if (guid == output3PortGuid && outputPorts.Count > 2)
                return outputPorts[2];
            if (guid == output4PortGuid && outputPorts.Count > 3)
                return outputPorts[3];
            return null;
        }
    }
}
