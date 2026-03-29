using System.Collections.Generic;
using UnityEngine;

namespace VNWinter.DialogueGraph
{
    /// <summary>
    /// ScriptableObject asset containing a complete dialogue graph.
    /// Created via the Dialogue Graph Editor (VNWinter menu) or right-click Create menu.
    /// Contains all nodes and connections needed for runtime dialogue playback.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "VNWinter/Dialogue Graph")]
    public class DialogueGraphAsset : ScriptableObject
    {
        // Node storage - each type stored in its own list for type-safe serialization
        [SerializeField] private StartNodeData startNode;
        [SerializeField] private List<DialogueNodeData> dialogueNodes = new List<DialogueNodeData>();
        [SerializeField] private List<ChoiceNodeData> choiceNodes = new List<ChoiceNodeData>();
        [SerializeField] private List<EndNodeData> endNodes = new List<EndNodeData>();
        [SerializeField] private List<RandomNodeData> randomNodes = new List<RandomNodeData>();
        [SerializeField] private List<VariableNodeData> variableNodes = new List<VariableNodeData>();
        [SerializeField] private List<ConditionalNodeData> conditionalNodes = new List<ConditionalNodeData>();
        [SerializeField] private List<EventNodeData> eventNodes = new List<EventNodeData>();
        [SerializeField] private List<RoomTransitionNodeData> roomTransitionNodes = new List<RoomTransitionNodeData>();
        [SerializeField] private List<FadeNodeData> fadeNodes = new List<FadeNodeData>();
        [SerializeField] private List<GraphTransitionNodeData> graphTransitionNodes = new List<GraphTransitionNodeData>();
        [SerializeField] private List<PreventAutoTriggerNodeData> preventAutoTriggerNodes = new List<PreventAutoTriggerNodeData>();
        [SerializeField] private List<SceneImageNodeData> sceneImageNodes = new List<SceneImageNodeData>();
        [SerializeField] private List<AudioNodeData> audioNodes = new List<AudioNodeData>();
        [SerializeField] private List<StopSoundNodeData> stopSoundNodes = new List<StopSoundNodeData>();
        [SerializeField] private List<SaveAudioPlaybackNodeData> saveAudioPlaybackNodes = new List<SaveAudioPlaybackNodeData>();
        [SerializeField] private List<NotLastCharacterNodeData> notLastCharacterNodes = new List<NotLastCharacterNodeData>();
        [SerializeField] private List<StartFrostingNodeData> startFrostingNodes = new List<StartFrostingNodeData>();
        [SerializeField] private List<NodeConnectionData> connections = new List<NodeConnectionData>();

        // Editor-only annotations (sticky notes) - not used at runtime
        [SerializeField] private List<StickyNoteData> stickyNotes = new List<StickyNoteData>();

        // Public read-only accessors for runtime use
        public StartNodeData StartNode => startNode;
        public IReadOnlyList<DialogueNodeData> DialogueNodes => dialogueNodes;
        public IReadOnlyList<ChoiceNodeData> ChoiceNodes => choiceNodes;
        public IReadOnlyList<EndNodeData> EndNodes => endNodes;
        public IReadOnlyList<RandomNodeData> RandomNodes => randomNodes;
        public IReadOnlyList<VariableNodeData> VariableNodes => variableNodes;
        public IReadOnlyList<ConditionalNodeData> ConditionalNodes => conditionalNodes;
        public IReadOnlyList<EventNodeData> EventNodes => eventNodes;
        public IReadOnlyList<RoomTransitionNodeData> RoomTransitionNodes => roomTransitionNodes;
        public IReadOnlyList<FadeNodeData> FadeNodes => fadeNodes;
        public IReadOnlyList<GraphTransitionNodeData> GraphTransitionNodes => graphTransitionNodes;
        public IReadOnlyList<PreventAutoTriggerNodeData> PreventAutoTriggerNodes => preventAutoTriggerNodes;
        public IReadOnlyList<SceneImageNodeData> SceneImageNodes => sceneImageNodes;
        public IReadOnlyList<AudioNodeData> AudioNodes => audioNodes;
        public IReadOnlyList<StopSoundNodeData> StopSoundNodes => stopSoundNodes;
        public IReadOnlyList<SaveAudioPlaybackNodeData> SaveAudioPlaybackNodes => saveAudioPlaybackNodes;
        public IReadOnlyList<NotLastCharacterNodeData> NotLastCharacterNodes => notLastCharacterNodes;
        public IReadOnlyList<StartFrostingNodeData> StartFrostingNodes => startFrostingNodes;
        public IReadOnlyList<NodeConnectionData> Connections => connections;
        public IReadOnlyList<StickyNoteData> StickyNotes => stickyNotes;

        #region Editor Methods (called by DialogueGraphSaveUtility)

        /// <summary>Sets the entry point node for this graph.</summary>
        public void SetStartNode(StartNodeData data) => startNode = data;

        /// <summary>Adds a dialogue node to the graph.</summary>
        public void AddDialogueNode(DialogueNodeData data) => dialogueNodes.Add(data);

        /// <summary>Adds a choice node to the graph.</summary>
        public void AddChoiceNode(ChoiceNodeData data) => choiceNodes.Add(data);

        /// <summary>Adds an end node to the graph.</summary>
        public void AddEndNode(EndNodeData data) => endNodes.Add(data);

        /// <summary>Adds a random branching node to the graph.</summary>
        public void AddRandomNode(RandomNodeData data) => randomNodes.Add(data);

        /// <summary>Adds a variable manipulation node to the graph.</summary>
        public void AddVariableNode(VariableNodeData data) => variableNodes.Add(data);

        /// <summary>Adds a conditional branching node to the graph.</summary>
        public void AddConditionalNode(ConditionalNodeData data) => conditionalNodes.Add(data);

        /// <summary>Adds an event trigger node to the graph.</summary>
        public void AddEventNode(EventNodeData data) => eventNodes.Add(data);
        public void AddRoomTransitionNode(RoomTransitionNodeData data) => roomTransitionNodes.Add(data);

        /// <summary>Adds a fade screen effect node to the graph.</summary>
        public void AddFadeNode(FadeNodeData data) => fadeNodes.Add(data);

        public void AddGraphTransitionNode(GraphTransitionNodeData data) => graphTransitionNodes.Add(data);

        /// <summary>Adds a prevent auto trigger node to the graph.</summary>
        public void AddPreventAutoTriggerNode(PreventAutoTriggerNodeData data) => preventAutoTriggerNodes.Add(data);

        /// <summary>Adds a scene image node to the graph.</summary>
        public void AddSceneImageNode(SceneImageNodeData data) => sceneImageNodes.Add(data);

        /// <summary>Adds an audio control node to the graph.</summary>
        public void AddAudioNode(AudioNodeData data) => audioNodes.Add(data);

        /// <summary>Adds a stop sound node to the graph.</summary>
        public void AddStopSoundNode(StopSoundNodeData data) => stopSoundNodes.Add(data);

        /// <summary>Adds a save audio playback node to the graph.</summary>
        public void AddSaveAudioPlaybackNode(SaveAudioPlaybackNodeData data) => saveAudioPlaybackNodes.Add(data);

        /// <summary>Adds a not last character node to the graph.</summary>
        public void AddNotLastCharacterNode(NotLastCharacterNodeData data) => notLastCharacterNodes.Add(data);

        /// <summary>Adds a start frosting node to the graph.</summary>
        public void AddStartFrostingNode(StartFrostingNodeData data) => startFrostingNodes.Add(data);

        /// <summary>Adds a connection between two nodes.</summary>
        public void AddConnection(NodeConnectionData connection) => connections.Add(connection);

        /// <summary>Adds a sticky note annotation to the graph.</summary>
        public void AddStickyNote(StickyNoteData data) => stickyNotes.Add(data);

        /// <summary>Removes a sticky note by its GUID.</summary>
        public void RemoveStickyNote(string guid) => stickyNotes.RemoveAll(n => n.guid == guid);

        /// <summary>Clears all sticky notes from the graph.</summary>
        public void ClearStickyNotes() => stickyNotes.Clear();

        /// <summary>
        /// Removes a node and all its connections from the graph.
        /// Searches all node lists by GUID.
        /// </summary>
        public void RemoveNode(string guid)
        {
            if (startNode != null && startNode.guid == guid)
            {
                startNode = null;
                return;
            }

            dialogueNodes.RemoveAll(n => n.guid == guid);
            choiceNodes.RemoveAll(n => n.guid == guid);
            endNodes.RemoveAll(n => n.guid == guid);
            randomNodes.RemoveAll(n => n.guid == guid);
            variableNodes.RemoveAll(n => n.guid == guid);
            conditionalNodes.RemoveAll(n => n.guid == guid);
            eventNodes.RemoveAll(n => n.guid == guid);
            roomTransitionNodes.RemoveAll(n => n.guid == guid);
            fadeNodes.RemoveAll(n => n.guid == guid);
            graphTransitionNodes.RemoveAll(n => n.guid == guid);
            preventAutoTriggerNodes.RemoveAll(n => n.guid == guid);
            sceneImageNodes.RemoveAll(n => n.guid == guid);
            audioNodes.RemoveAll(n => n.guid == guid);
            stopSoundNodes.RemoveAll(n => n.guid == guid);
            saveAudioPlaybackNodes.RemoveAll(n => n.guid == guid);
            notLastCharacterNodes.RemoveAll(n => n.guid == guid);
            startFrostingNodes.RemoveAll(n => n.guid == guid);

            // Also remove any connections to/from this node
            connections.RemoveAll(c => c.outputNodeGuid == guid || c.inputNodeGuid == guid);
        }

        /// <summary>Removes a specific connection by its output node and port.</summary>
        public void RemoveConnection(string outputGuid, string outputPort)
        {
            connections.RemoveAll(c => c.outputNodeGuid == outputGuid && c.outputPortName == outputPort);
        }

        /// <summary>Clears all nodes, connections, and sticky notes. Called before loading a new graph.</summary>
        public void ClearAll()
        {
            startNode = null;
            dialogueNodes.Clear();
            choiceNodes.Clear();
            endNodes.Clear();
            randomNodes.Clear();
            variableNodes.Clear();
            conditionalNodes.Clear();
            eventNodes.Clear();
            roomTransitionNodes.Clear();
            fadeNodes.Clear();
            graphTransitionNodes.Clear();
            preventAutoTriggerNodes.Clear();
            sceneImageNodes.Clear();
            audioNodes.Clear();
            stopSoundNodes.Clear();
            saveAudioPlaybackNodes.Clear();
            notLastCharacterNodes.Clear();
            startFrostingNodes.Clear();
            connections.Clear();
            stickyNotes.Clear();
        }

        #endregion

        #region Runtime Lookup Methods

        /// <summary>
        /// Finds a node by its GUID across all node type lists.
        /// Used by DialogueManager to traverse the graph during playback.
        /// </summary>
        /// <returns>The node data, or null if not found.</returns>
        public BaseNodeData GetNodeByGuid(string guid)
        {
            if (startNode != null && startNode.guid == guid)
                return startNode;

            foreach (var node in dialogueNodes)
                if (node.guid == guid) return node;

            foreach (var node in choiceNodes)
                if (node.guid == guid) return node;

            foreach (var node in endNodes)
                if (node.guid == guid) return node;

            foreach (var node in randomNodes)
                if (node.guid == guid) return node;

            foreach (var node in variableNodes)
                if (node.guid == guid) return node;

            foreach (var node in conditionalNodes)
                if (node.guid == guid) return node;

            foreach (var node in eventNodes)
                if (node.guid == guid) return node;
            foreach (var node in roomTransitionNodes)
                if (node.guid == guid) return node;
            foreach (var node in fadeNodes)
                if (node.guid == guid) return node;
            foreach (var node in graphTransitionNodes)
                if (node.guid == guid) return node;
            foreach (var node in preventAutoTriggerNodes)
                if (node.guid == guid) return node;
            foreach (var node in sceneImageNodes)
                if (node.guid == guid) return node;
            foreach (var node in audioNodes)
                if (node.guid == guid) return node;
            foreach (var node in stopSoundNodes)
                if (node.guid == guid) return node;

            foreach (var node in saveAudioPlaybackNodes)
                if (node.guid == guid) return node;

            foreach (var node in notLastCharacterNodes)
                if (node.guid == guid) return node;

            foreach (var node in startFrostingNodes)
                if (node.guid == guid) return node;

            return null;
        }

        /// <summary>
        /// Gets all connections originating from a specific node.
        /// Used to find the next node(s) when advancing dialogue.
        /// </summary>
        public List<NodeConnectionData> GetConnectionsFromNode(string guid)
        {
            return connections.FindAll(c => c.outputNodeGuid == guid);
        }

        /// <summary>
        /// Gets the connection leading into a specific node.
        /// Useful for backtracking or validation.
        /// </summary>
        public NodeConnectionData GetConnectionToNode(string guid)
        {
            return connections.Find(c => c.inputNodeGuid == guid);
        }

        #endregion
    }
}
