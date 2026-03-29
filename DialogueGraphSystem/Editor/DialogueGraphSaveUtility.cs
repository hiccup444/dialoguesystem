using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace VNWinter.DialogueGraph.Editor
{
    public static class DialogueGraphSaveUtility
    {
        public static void SaveGraph(DialogueGraphView graphView, DialogueGraphAsset asset, bool recordUndo = true)
        {
            if (asset == null)
            {
                Debug.LogError("Cannot save: No asset assigned");
                return;
            }

            if (recordUndo)
            {
                Undo.RecordObject(asset, "Save Dialogue Graph");
            }

            asset.ClearAll();

            var nodes = graphView.GetAllNodes();
            foreach (var node in nodes)
            {
                var data = node.SaveToData();

                switch (data)
                {
                    case StartNodeData startData:
                        asset.SetStartNode(startData);
                        break;
                    case DialogueNodeData dialogueData:
                        asset.AddDialogueNode(dialogueData);
                        break;
                case ChoiceNodeData choiceData:
                    asset.AddChoiceNode(choiceData);
                    break;
                case EndNodeData endData:
                    asset.AddEndNode(endData);
                    break;
                case RandomNodeData randomData:
                    asset.AddRandomNode(randomData);
                    break;
                case VariableNodeData variableData:
                    asset.AddVariableNode(variableData);
                    break;
                case ConditionalNodeData conditionalData:
                    asset.AddConditionalNode(conditionalData);
                    break;
                case EventNodeData eventData:
                    asset.AddEventNode(eventData);
                    break;
                case RoomTransitionNodeData transitionData:
                    asset.AddRoomTransitionNode(transitionData);
                    break;
                case FadeNodeData fadeData:
                    asset.AddFadeNode(fadeData);
                    break;
                case GraphTransitionNodeData graphData:
                    asset.AddGraphTransitionNode(graphData);
                    break;
                case PreventAutoTriggerNodeData preventAutoTriggerData:
                    asset.AddPreventAutoTriggerNode(preventAutoTriggerData);
                    break;
                case SceneImageNodeData sceneImageData:
                    asset.AddSceneImageNode(sceneImageData);
                    break;
                case AudioNodeData audioData:
                    asset.AddAudioNode(audioData);
                    break;
                case StopSoundNodeData stopSoundData:
                    asset.AddStopSoundNode(stopSoundData);
                    break;
                case SaveAudioPlaybackNodeData saveAudioPlaybackData:
                    asset.AddSaveAudioPlaybackNode(saveAudioPlaybackData);
                    break;
                case NotLastCharacterNodeData notLastCharacterData:
                    asset.AddNotLastCharacterNode(notLastCharacterData);
                    break;
                case StartFrostingNodeData startFrostingData:
                    asset.AddStartFrostingNode(startFrostingData);
                    break;
            }
        }

            var edges = graphView.GetAllEdges();
            foreach (var edge in edges)
            {
                var outputNode = edge.output.node as BaseDialogueNode;
                var inputNode = edge.input.node as BaseDialogueNode;

                if (outputNode != null && inputNode != null)
                {
                    string outputPortName = edge.output.portName;

                    // For ChoiceNode, convert "Choice X" back to the actual GUID
                    if (outputNode is ChoiceNode choiceNode)
                    {
                        // Extract index from "Choice X" (1-based)
                        if (outputPortName.StartsWith("Choice ") &&
                            int.TryParse(outputPortName.Substring(7), out int choiceNumber))
                        {
                            outputPortName = choiceNode.GetGuidByIndex(choiceNumber - 1) ?? outputPortName;
                        }
                    }

                    // For ConditionalNode, convert "True"/"False" to actual GUID
                    if (outputNode is ConditionalNode conditionalNode)
                    {
                        if (outputPortName == "True")
                            outputPortName = conditionalNode.GetTruePortGuid();
                        else if (outputPortName == "False")
                            outputPortName = conditionalNode.GetFalsePortGuid();
                    }

                    // For RandomNode, convert "Output X" to actual GUID
                    if (outputNode is RandomNode randomNode)
                    {
                        if (outputPortName.StartsWith("Output ") &&
                            int.TryParse(outputPortName.Substring(7), out int outputNumber))
                        {
                            outputPortName = randomNode.GetPortGuid(outputNumber - 1) ?? outputPortName;
                        }
                    }

                    asset.AddConnection(new NodeConnectionData
                    {
                        outputNodeGuid = outputNode.Guid,
                        outputPortName = outputPortName,
                        inputNodeGuid = inputNode.Guid,
                        inputPortName = edge.input.portName
                    });
                }
            }

            // Save sticky notes
            var stickyNotes = graphView.GetAllStickyNotes();
            foreach (var stickyNote in stickyNotes)
            {
                asset.AddStickyNote(stickyNote.SaveToData());
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log($"Saved dialogue graph to {AssetDatabase.GetAssetPath(asset)}");
        }

        public static void LoadGraph(DialogueGraphView graphView, DialogueGraphAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("Cannot load: No asset assigned");
                return;
            }

            graphView.ClearGraph();

            var nodeMap = new Dictionary<string, BaseDialogueNode>();

            // Create or load StartNode
            if (asset.StartNode == null || string.IsNullOrEmpty(asset.StartNode.guid))
            {
                // No valid StartNode exists - create a new one
                var startNode = new StartNode();
                startNode.Initialize(Vector2.zero);
                graphView.AddElement(startNode);
                nodeMap[startNode.Guid] = startNode;
            }
            else
            {
                // Load existing StartNode
                var node = graphView.CreateNodeFromData(asset.StartNode);
                nodeMap[asset.StartNode.guid] = node;
            }

            foreach (var data in asset.DialogueNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.ChoiceNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.EndNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.RandomNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.VariableNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.ConditionalNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.EventNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.RoomTransitionNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.FadeNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.GraphTransitionNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.PreventAutoTriggerNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.SceneImageNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.AudioNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.StopSoundNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.SaveAudioPlaybackNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.NotLastCharacterNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var data in asset.StartFrostingNodes)
            {
                var node = graphView.CreateNodeFromData(data);
                if (!string.IsNullOrEmpty(data.guid))
                {
                    nodeMap[data.guid] = node;
                }
            }

            foreach (var connection in asset.Connections)
            {
                if (!nodeMap.TryGetValue(connection.outputNodeGuid, out var outputNode)) continue;
                if (!nodeMap.TryGetValue(connection.inputNodeGuid, out var inputNode)) continue;

                Port outputPort = null;
                Port inputPort = null;

                if (outputNode is ChoiceNode choiceNode)
                {
                    outputPort = choiceNode.GetOutputPortByGuid(connection.outputPortName);
                }
                else if (outputNode is ConditionalNode conditionalNode)
                {
                    outputPort = conditionalNode.GetOutputPortByGuid(connection.outputPortName);
                }
                else if (outputNode is RandomNode randomNode)
                {
                    outputPort = randomNode.GetOutputPortByGuid(connection.outputPortName);
                }
                else
                {
                    outputPort = outputNode.GetOutputPort();
                }

                inputPort = inputNode.GetInputPort();

                if (outputPort != null && inputPort != null)
                {
                    var edge = outputPort.ConnectTo(inputPort);
                    graphView.AddElement(edge);
                }
            }

            // Load sticky notes
            foreach (var stickyNoteData in asset.StickyNotes)
            {
                graphView.CreateStickyNoteFromData(stickyNoteData);
            }
        }
    }
}
