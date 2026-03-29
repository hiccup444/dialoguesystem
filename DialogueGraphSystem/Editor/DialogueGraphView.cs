using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace VNWinter.DialogueGraph.Editor
{
    /// <summary>
    /// The main graph view canvas for editing dialogue graphs.
    /// Handles node creation, connections, context menus, and graph manipulation.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        private MiniMap miniMap;

        public DialogueGraphView()
        {
            // Enable standard graph view interactions
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Add grid background for visual reference
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddMiniMap();

            // Load custom styling if available
            var styleSheet = Resources.Load<StyleSheet>("DialogueGraphStyles");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            graphViewChanged += OnGraphViewChanged;

            // Clipboard support
            serializeGraphElements = SerializeGraphElements;
            canPasteSerializedData = CanPasteSerializedData;
            unserializeAndPaste = (operationName, data) => PasteSerializedData(data, new Vector2(30, 30));
        }

        [System.Serializable]
        private class GraphCopyData
        {
            [System.Serializable]
            public class SerializedNode
            {
                public string type;
                public string json;
            }

            public List<SerializedNode> nodes = new();
            public List<NodeConnectionData> edges = new();
            public List<StickyNoteData> stickyNotes = new();
        }

        /// <summary>
        /// Handles graph changes, particularly auto-filling new dialogue nodes
        /// with speaker/portrait from connected predecessor.
        /// </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var inputNode = edge.input.node as BaseDialogueNode;
                    var outputNode = edge.output.node as BaseDialogueNode;

                    // Auto-fill speaker/portrait when connecting to a dialogue node
                    if (inputNode is DialogueNode dialogueNode && outputNode != null)
                    {
                        AutoFillDialogueNode(dialogueNode, outputNode);
                    }
                }
            }

            return graphViewChange;
        }

        /// <summary>
        /// Copies speaker name and portrait from the previous dialogue node in the chain.
        /// Saves writers from re-entering the same character info repeatedly.
        /// </summary>
        private void AutoFillDialogueNode(DialogueNode targetNode, BaseDialogueNode sourceNode)
        {
            var (speaker, portrait) = FindLastSpeakerAndPortrait(sourceNode, new HashSet<string>());
            if (!string.IsNullOrEmpty(speaker) || portrait != null)
            {
                targetNode.SetSpeakerAndPortrait(speaker, portrait);
            }
        }

        /// <summary>
        /// Recursively traverses backwards through the graph to find the most recent
        /// dialogue node with speaker/portrait information.
        /// </summary>
        private (string speaker, UnityEngine.Sprite portrait) FindLastSpeakerAndPortrait(BaseDialogueNode node, HashSet<string> visited)
        {
            if (node == null || visited.Contains(node.Guid))
                return (null, null);

            visited.Add(node.Guid);

            if (node is DialogueNode dialogueNode)
            {
                return (dialogueNode.SpeakerName, dialogueNode.Portrait);
            }

            // Traverse backwards through incoming edges
            var inputPort = node.GetInputPort();
            if (inputPort != null)
            {
                foreach (var edge in inputPort.connections)
                {
                    var previousNode = edge.output.node as BaseDialogueNode;
                    if (previousNode != null)
                    {
                        var result = FindLastSpeakerAndPortrait(previousNode, visited);
                        if (!string.IsNullOrEmpty(result.speaker) || result.portrait != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return (null, null);
        }

        private void AddMiniMap()
        {
            miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            Add(miniMap);
        }

        /// <summary>Shows or hides the mini-map navigation overlay.</summary>
        public void ToggleMiniMap(bool visible)
        {
            miniMap.visible = visible;
        }

        /// <summary>
        /// Builds the right-click context menu for creating new nodes.
        /// Start node is disabled if one already exists.
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView)
            {
                var mousePosition = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

                evt.menu.AppendAction("Create Start Node",
                    action => CreateNode<StartNode>(mousePosition),
                    HasStartNode() ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                evt.menu.AppendAction("Create Dialogue Node",
                    action => CreateNode<DialogueNode>(mousePosition));

                evt.menu.AppendAction("Create Choice Node",
                    action => CreateNode<ChoiceNode>(mousePosition));

                evt.menu.AppendAction("Create End Node",
                    action => CreateNode<EndNode>(mousePosition));

                evt.menu.AppendAction("Create Random Node",
                    action => CreateNode<RandomNode>(mousePosition));

                evt.menu.AppendAction("Create Variable Node",
                    action => CreateNode<VariableNode>(mousePosition));

                evt.menu.AppendAction("Create Conditional Node",
                    action => CreateNode<ConditionalNode>(mousePosition));

                evt.menu.AppendAction("Create Event Node",
                    action => CreateNode<EventNode>(mousePosition));

                evt.menu.AppendAction("Create Room Transition Node",
                    action => CreateNode<RoomTransitionNode>(mousePosition));

                evt.menu.AppendAction("Create Fade Node",
                    action => CreateNode<FadeNode>(mousePosition));

                evt.menu.AppendAction("Create Graph Transition Node",
                    action => CreateNode<GraphTransitionNode>(mousePosition));

                evt.menu.AppendAction("Create Prevent Auto Trigger Node",
                    action => CreateNode<PreventAutoTriggerNode>(mousePosition));

                evt.menu.AppendAction("Create Scene Image Node",
                    action => CreateNode<SceneImageNode>(mousePosition));

                evt.menu.AppendAction("Create Audio Node",
                    action => CreateNode<AudioNode>(mousePosition));

                evt.menu.AppendAction("Create Stop Sound Node",
                    action => CreateNode<StopSoundNode>(mousePosition));

                evt.menu.AppendAction("Create Save Audio Playback Node",
                    action => CreateNode<SaveAudioPlaybackNode>(mousePosition));

                evt.menu.AppendAction("Create Not Last Character Node",
                    action => CreateNode<NotLastCharacterNode>(mousePosition));

                evt.menu.AppendAction("Create Start Frosting Node",
                    action => CreateNode<StartFrostingNode>(mousePosition));

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Create Sticky Note",
                    action => CreateStickyNote(mousePosition));
            }

            base.BuildContextualMenu(evt);
        }

        private bool HasStartNode()
        {
            return nodes.ToList().Any(n => n is StartNode);
        }

        /// <summary>
        /// Determines which ports can connect to each other.
        /// Prevents self-connections and mismatched port directions.
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                // Can't connect to self
                if (startPort.node == port.node) return;
                // Can't connect output to output or input to input
                if (startPort.direction == port.direction) return;
                // Must be same port type
                if (startPort.portType != port.portType) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        private new string SerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var copyData = new GraphCopyData();

            var selectedNodes = elements.OfType<BaseDialogueNode>().ToList();
            var selectedNotes = elements.OfType<StickyNote>().ToList();
            var selectedEdges = elements.OfType<Edge>().ToList();

            foreach (var node in selectedNodes)
            {
                var data = node.SaveToData();
                var serialized = new GraphCopyData.SerializedNode
                {
                    type = data.GetType().AssemblyQualifiedName,
                    json = JsonUtility.ToJson(data)
                };
                copyData.nodes.Add(serialized);
            }

            foreach (var note in selectedNotes)
            {
                var data = note.SaveToData();
                copyData.stickyNotes.Add(CloneStickyNoteData(data));
            }

            // Only copy edges where both ends are included
            foreach (var edge in selectedEdges)
            {
                if (edge.output?.node is not BaseDialogueNode outputNode) continue;
                if (edge.input?.node is not BaseDialogueNode inputNode) continue;
                if (!selectedNodes.Contains(outputNode) || !selectedNodes.Contains(inputNode)) continue;

                string outputPortName = edge.output.portName;

                if (outputNode is ChoiceNode choiceNode)
                {
                    if (outputPortName.StartsWith("Choice ") &&
                        int.TryParse(outputPortName.Substring(7), out int choiceNumber))
                    {
                        outputPortName = choiceNode.GetGuidByIndex(choiceNumber - 1) ?? outputPortName;
                    }
                }
                else if (outputNode is ConditionalNode conditionalNode)
                {
                    if (outputPortName == "True")
                        outputPortName = conditionalNode.GetTruePortGuid();
                    else if (outputPortName == "False")
                        outputPortName = conditionalNode.GetFalsePortGuid();
                }

                copyData.edges.Add(new NodeConnectionData
                {
                    outputNodeGuid = outputNode.Guid,
                    outputPortName = outputPortName,
                    inputNodeGuid = inputNode.Guid,
                    inputPortName = edge.input.portName
                });
            }

            return JsonUtility.ToJson(copyData, true);
        }

        private new bool CanPasteSerializedData(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;
            try
            {
                var parsed = JsonUtility.FromJson<GraphCopyData>(data);
                return parsed != null && parsed.nodes != null && parsed.nodes.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void PasteSerializedData(string data, Vector2 offset)
        {
            if (!CanPasteSerializedData(data)) return;

            var copyData = JsonUtility.FromJson<GraphCopyData>(data);
            if (copyData == null || copyData.nodes.Count == 0) return;

            var guidMap = new Dictionary<string, string>();
            var newNodes = new Dictionary<string, BaseDialogueNode>();

            // Deserialize node data with proper types
            var deserializedNodes = new List<BaseNodeData>();
            foreach (var nodeEntry in copyData.nodes)
            {
                var nodeType = System.Type.GetType(nodeEntry.type);
                if (nodeType == null) continue;
                var nodeData = JsonUtility.FromJson(nodeEntry.json, nodeType) as BaseNodeData;
                if (nodeData != null)
                    deserializedNodes.Add(nodeData);
            }

            if (deserializedNodes.Count == 0) return;

            // Compute paste offset based on average position
            Vector2 average = Vector2.zero;
            foreach (var node in deserializedNodes)
            {
                average += node.position;
            }
            average /= deserializedNodes.Count;

            foreach (var nodeData in deserializedNodes)
            {
                var oldGuid = nodeData.guid;
                nodeData.guid = System.Guid.NewGuid().ToString();
                nodeData.position = nodeData.position - average + offset;

                var newNode = CreateNodeFromData(nodeData);
                guidMap[oldGuid] = nodeData.guid;
                newNodes[nodeData.guid] = newNode;
            }

            foreach (var noteData in copyData.stickyNotes)
            {
                var oldGuid = noteData.guid;
                noteData.guid = System.Guid.NewGuid().ToString();
                noteData.position = noteData.position - average + offset;
                CreateStickyNoteFromData(noteData);
                guidMap[oldGuid] = noteData.guid;
            }

            foreach (var edgeData in copyData.edges)
            {
                if (!guidMap.TryGetValue(edgeData.outputNodeGuid, out var newOutputGuid)) continue;
                if (!guidMap.TryGetValue(edgeData.inputNodeGuid, out var newInputGuid)) continue;

                var outputNode = newNodes[newOutputGuid];
                var inputNode = newNodes[newInputGuid];

                Port outputPort = null;
                if (outputNode is ChoiceNode choiceNode)
                {
                    outputPort = choiceNode.GetOutputPortByGuid(edgeData.outputPortName);
                }
                else if (outputNode is ConditionalNode conditionalNode)
                {
                    outputPort = conditionalNode.GetOutputPortByGuid(edgeData.outputPortName);
                }
                else
                {
                    outputPort = outputNode.GetOutputPort();
                }

                var inputPort = inputNode.GetInputPort();

                if (outputPort != null && inputPort != null)
                {
                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        }

        public void DuplicateSelection()
        {
            var selected = selection.OfType<GraphElement>().ToList();
            if (selected.Count == 0) return;

            // Compute average position of current selection so duplicates land nearby
            Vector2 avg = Vector2.zero;
            int count = 0;
            foreach (var element in selected)
            {
                var rect = element.GetPosition();
                avg += rect.position;
                count++;
            }
            if (count > 0) avg /= count;

            var data = SerializeGraphElements(selected);
            // Nudge slightly down-right from the original cluster
            PasteSerializedData(data, avg + new Vector2(40f, 40f));
        }

        public void CopySelection()
        {
            var data = SerializeGraphElements(selection.OfType<GraphElement>());
            EditorGUIUtility.systemCopyBuffer = data;
        }

        public void Paste()
        {
            var data = EditorGUIUtility.systemCopyBuffer;
            PasteSerializedData(data, new Vector2(30, 30));
        }

        private static StickyNoteData CloneStickyNoteData(StickyNoteData data)
        {
            var json = JsonUtility.ToJson(data);
            return JsonUtility.FromJson<StickyNoteData>(json);
        }

        /// <summary>Creates a new node of the specified type at the given position.</summary>
        public T CreateNode<T>(Vector2 position) where T : BaseDialogueNode, new()
        {
            var node = new T();
            node.Initialize(position);
            AddElement(node);
            return node;
        }

        /// <summary>
        /// Creates a node from saved data. Used when loading a graph from asset.
        /// </summary>
        public BaseDialogueNode CreateNodeFromData(BaseNodeData data)
        {
            BaseDialogueNode node = data.nodeType switch
            {
                "Start" => new StartNode(),
                "Dialogue" => new DialogueNode(),
                "Choice" => new ChoiceNode(),
                "End" => new EndNode(),
                "Random" => new RandomNode(),
                "Variable" => new VariableNode(),
                "Conditional" => new ConditionalNode(),
                "Event" => new EventNode(),
                "RoomTransition" => new RoomTransitionNode(),
                "Fade" => new FadeNode(),
                "GraphTransition" => new GraphTransitionNode(),
                "PreventAutoTrigger" => new PreventAutoTriggerNode(),
                "SceneImage" => new SceneImageNode(),
                "Audio" => new AudioNode(),
                "StopSound" => new StopSoundNode(),
                "SaveAudioPlayback" => new SaveAudioPlaybackNode(),
                "NotLastCharacter" => new NotLastCharacterNode(),
                "StartFrosting" => new StartFrostingNode(),
                _ => throw new ArgumentException($"Unknown node type: {data.nodeType}")
            };

            node.LoadFromData(data);
            AddElement(node);
            return node;
        }

        /// <summary>Removes all nodes, edges, and sticky notes from the graph.</summary>
        public void ClearGraph()
        {
            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }

            foreach (var node in nodes.ToList())
            {
                RemoveElement(node);
            }

            foreach (var stickyNote in GetAllStickyNotes())
            {
                RemoveElement(stickyNote);
            }
        }

        /// <summary>Returns all dialogue nodes currently in the graph.</summary>
        public List<BaseDialogueNode> GetAllNodes()
        {
            return nodes.ToList().OfType<BaseDialogueNode>().ToList();
        }

        /// <summary>Returns all edge connections currently in the graph.</summary>
        public List<Edge> GetAllEdges()
        {
            return edges.ToList();
        }

        /// <summary>Returns all sticky notes currently in the graph.</summary>
        public List<StickyNote> GetAllStickyNotes()
        {
            return graphElements.ToList().OfType<StickyNote>().ToList();
        }

        /// <summary>Creates a new sticky note at the specified position.</summary>
        public StickyNote CreateStickyNote(Vector2 position)
        {
            var stickyNote = new StickyNote();
            stickyNote.Initialize(position);
            AddElement(stickyNote);
            return stickyNote;
        }

        /// <summary>Creates a sticky note from saved data. Used when loading a graph.</summary>
        public StickyNote CreateStickyNoteFromData(StickyNoteData data)
        {
            var stickyNote = new StickyNote();
            stickyNote.LoadFromData(data);
            AddElement(stickyNote);
            return stickyNote;
        }

        /// <summary>
        /// Selects a node by GUID and centers the view on it.
        /// Used by search results and validation panel.
        /// </summary>
        public void FocusOnNode(string guid)
        {
            var targetNode = nodes.ToList()
                .OfType<BaseDialogueNode>()
                .FirstOrDefault(n => n.Guid == guid);

            if (targetNode == null) return;

            ClearSelection();
            AddToSelection(targetNode);
            FrameSelection();
        }

        /// <summary>
        /// Searches all nodes for matching content based on search type.
        /// Returns list of matching nodes for display in search panel.
        /// </summary>
        public List<BaseDialogueNode> SearchNodes(string query, SearchType searchType)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<BaseDialogueNode>();

            query = query.ToLowerInvariant();
            var results = new List<BaseDialogueNode>();

            foreach (var node in nodes.ToList().OfType<BaseDialogueNode>())
            {
                bool matches = false;

                switch (searchType)
                {
                    case SearchType.All:
                        matches = MatchesAnyField(node, query);
                        break;
                    case SearchType.Speaker:
                        if (node is DialogueNode dialogueNode)
                            matches = dialogueNode.SpeakerName?.ToLowerInvariant().Contains(query) ?? false;
                        break;
                    case SearchType.DialogueText:
                        if (node is DialogueNode dn)
                            matches = dn.DialogueText?.ToLowerInvariant().Contains(query) ?? false;
                        break;
                    case SearchType.VariableName:
                        if (node is VariableNode vn)
                            matches = vn.VariableName?.ToLowerInvariant().Contains(query) ?? false;
                        else if (node is ConditionalNode cn)
                            matches = cn.VariableName?.ToLowerInvariant().Contains(query) ?? false;
                        break;
                    case SearchType.GUID:
                        // Support both full GUID and shortened (first 8 characters) format
                        matches = node.Guid?.ToLowerInvariant().Contains(query) ?? false;
                        break;
                }

                if (matches)
                    results.Add(node);
            }

            return results;
        }

        /// <summary>Checks if node contains query string in any searchable field.</summary>
        private bool MatchesAnyField(BaseDialogueNode node, string query)
        {
            // Check GUID first (works for all node types)
            if (node.Guid?.ToLowerInvariant().Contains(query) ?? false)
                return true;

            if (node is DialogueNode dialogueNode)
            {
                return (dialogueNode.SpeakerName?.ToLowerInvariant().Contains(query) ?? false) ||
                       (dialogueNode.DialogueText?.ToLowerInvariant().Contains(query) ?? false);
            }
            if (node is VariableNode variableNode)
            {
                return variableNode.VariableName?.ToLowerInvariant().Contains(query) ?? false;
            }
            if (node is ConditionalNode conditionalNode)
            {
                return conditionalNode.VariableName?.ToLowerInvariant().Contains(query) ?? false;
            }
            if (node is ChoiceNode choiceNode)
            {
                var data = choiceNode.SaveToData() as ChoiceNodeData;
                if (data?.choices != null)
                {
                    return data.choices.Any(c => c.choiceText?.ToLowerInvariant().Contains(query) ?? false);
                }
            }
            return false;
        }
    }

    /// <summary>Filter options for the node search functionality.</summary>
    public enum SearchType
    {
        /// <summary>Search all text fields.</summary>
        All,
        /// <summary>Search only speaker names.</summary>
        Speaker,
        /// <summary>Search only dialogue text content.</summary>
        DialogueText,
        /// <summary>Search variable/conditional node variable names.</summary>
        VariableName,
        /// <summary>Search node GUIDs (supports both full GUID and 8-character shortened format).</summary>
        GUID
    }
}
