using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    /// <summary>
    /// Severity level for validation issues.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Critical issue that will cause runtime errors.</summary>
        Error,
        /// <summary>Potential issue that may cause unexpected behavior.</summary>
        Warning,
        /// <summary>Informational notice, not a problem.</summary>
        Info
    }

    /// <summary>
    /// Represents a single validation issue found in a dialogue graph.
    /// </summary>
    public class ValidationResult
    {
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; }
        /// <summary>GUID of the problematic node, if applicable. Used for "Go to Node" functionality.</summary>
        public string NodeGuid { get; set; }

        public ValidationResult(ValidationSeverity severity, string message, string nodeGuid = null)
        {
            Severity = severity;
            Message = message;
            NodeGuid = nodeGuid;
        }
    }

    /// <summary>
    /// Static validator for dialogue graphs. Checks for common issues like:
    /// - Missing start node
    /// - Orphaned/unreachable nodes
    /// - Empty choices or dialogue text
    /// - Unconnected conditional branches
    /// - Dead-end nodes (no outgoing connections)
    /// </summary>
    public static class DialogueGraphValidator
    {
        /// <summary>
        /// Validates a dialogue graph asset and returns all issues found.
        /// </summary>
        /// <param name="asset">The graph to validate.</param>
        /// <returns>List of validation results, may be empty if graph is valid.</returns>
        public static List<ValidationResult> Validate(DialogueGraphAsset asset)
        {
            var results = new List<ValidationResult>();

            if (asset == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, "Asset is null"));
                return results;
            }

            // Check: Missing start node
            if (asset.StartNode == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, "Graph has no Start node"));
            }

            // Collect all node GUIDs
            var allNodes = GetAllNodes(asset);
            var nodeGuids = new HashSet<string>(allNodes.Select(n => n.guid));

            // Check: Orphaned nodes (no incoming connections, except Start)
            var nodesWithIncomingConnections = new HashSet<string>();
            foreach (var conn in asset.Connections)
            {
                nodesWithIncomingConnections.Add(conn.inputNodeGuid);
            }

            foreach (var node in allNodes)
            {
                if (node is StartNodeData) continue;

                if (!nodesWithIncomingConnections.Contains(node.guid))
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Warning,
                        $"Orphaned node: '{GetNodeDisplayName(node)}' has no incoming connections",
                        node.guid));
                }
            }

            // Check: Unreachable nodes (not reachable from Start via BFS)
            if (asset.StartNode != null)
            {
                var reachableNodes = FindReachableNodes(asset);
                foreach (var node in allNodes)
                {
                    if (!reachableNodes.Contains(node.guid))
                    {
                        results.Add(new ValidationResult(
                            ValidationSeverity.Warning,
                            $"Unreachable node: '{GetNodeDisplayName(node)}' cannot be reached from Start",
                            node.guid));
                    }
                }
            }

            // Check: Choice nodes with 0 choices
            foreach (var choiceNode in asset.ChoiceNodes)
            {
                if (choiceNode.choices == null || choiceNode.choices.Count == 0)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Error,
                        "Choice node has no choices defined",
                        choiceNode.guid));
                }
                else
                {
                    // Check for empty choice text
                    for (int i = 0; i < choiceNode.choices.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(choiceNode.choices[i].choiceText))
                        {
                            results.Add(new ValidationResult(
                                ValidationSeverity.Warning,
                                $"Choice node has empty text for choice {i + 1}",
                                choiceNode.guid));
                        }
                    }
                }
            }

            // Check: Empty variable names in Variable/Conditional nodes
            foreach (var variableNode in asset.VariableNodes)
            {
                if (string.IsNullOrWhiteSpace(variableNode.variableName))
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Error,
                        "Variable node has no variable name specified",
                        variableNode.guid));
                }
            }

            foreach (var conditionalNode in asset.ConditionalNodes)
            {
                if (string.IsNullOrWhiteSpace(conditionalNode.variableName))
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Error,
                        "Conditional node has no variable name specified",
                        conditionalNode.guid));
                }
            }

            // Check: Dead ends (no outgoing connection, except End nodes)
            var nodesWithOutgoingConnections = new HashSet<string>();
            foreach (var conn in asset.Connections)
            {
                nodesWithOutgoingConnections.Add(conn.outputNodeGuid);
            }

            foreach (var node in allNodes)
            {
                if (node is EndNodeData) continue;

                if (!nodesWithOutgoingConnections.Contains(node.guid))
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Warning,
                        $"Dead end: '{GetNodeDisplayName(node)}' has no outgoing connections",
                        node.guid));
                }
            }

            // Check: Conditional nodes without both True and False connections
            foreach (var conditionalNode in asset.ConditionalNodes)
            {
                var connections = asset.GetConnectionsFromNode(conditionalNode.guid);
                bool hasTrueConnection = connections.Any(c => c.outputPortName == conditionalNode.truePortGuid);
                bool hasFalseConnection = connections.Any(c => c.outputPortName == conditionalNode.falsePortGuid);

                if (!hasTrueConnection)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Warning,
                        "Conditional node has no True branch connected",
                        conditionalNode.guid));
                }
                if (!hasFalseConnection)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Warning,
                        "Conditional node has no False branch connected",
                        conditionalNode.guid));
                }
            }

            // Check: Empty dialogue text
            foreach (var dialogueNode in asset.DialogueNodes)
            {
                if (string.IsNullOrWhiteSpace(dialogueNode.dialogueText))
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Info,
                        $"Dialogue node has empty text (Speaker: {dialogueNode.speakerName ?? "unnamed"})",
                        dialogueNode.guid));
                }
            }

            return results;
        }

        /// <summary>Collects all nodes from all node type lists into a single list.</summary>
        private static List<BaseNodeData> GetAllNodes(DialogueGraphAsset asset)
        {
            var nodes = new List<BaseNodeData>();

            if (asset.StartNode != null)
                nodes.Add(asset.StartNode);

            nodes.AddRange(asset.DialogueNodes);
            nodes.AddRange(asset.ChoiceNodes);
            nodes.AddRange(asset.EndNodes);
            nodes.AddRange(asset.VariableNodes);
            nodes.AddRange(asset.ConditionalNodes);
            nodes.AddRange(asset.EventNodes);

            return nodes;
        }

        /// <summary>
        /// Uses BFS to find all nodes reachable from the Start node.
        /// Used to detect orphaned/unreachable nodes.
        /// </summary>
        private static HashSet<string> FindReachableNodes(DialogueGraphAsset asset)
        {
            var reachable = new HashSet<string>();
            var queue = new Queue<string>();

            if (asset.StartNode == null) return reachable;

            queue.Enqueue(asset.StartNode.guid);

            while (queue.Count > 0)
            {
                var currentGuid = queue.Dequeue();

                if (reachable.Contains(currentGuid)) continue;
                reachable.Add(currentGuid);

                // Follow all outgoing connections
                var connections = asset.GetConnectionsFromNode(currentGuid);
                foreach (var conn in connections)
                {
                    if (!reachable.Contains(conn.inputNodeGuid))
                    {
                        queue.Enqueue(conn.inputNodeGuid);
                    }
                }
            }

            return reachable;
        }

        /// <summary>Returns a human-readable display name for a node.</summary>
        private static string GetNodeDisplayName(BaseNodeData node)
        {
            return node switch
            {
                StartNodeData => "Start",
                DialogueNodeData dn => $"Dialogue ({dn.speakerName ?? "unnamed"})",
                ChoiceNodeData => "Choice",
                EndNodeData => "End",
                VariableNodeData vn => $"Variable ({vn.variableName ?? "unnamed"})",
                ConditionalNodeData cn => $"Conditional ({cn.variableName ?? "unnamed"})",
                EventNodeData en => $"Event ({en.eventType})",
                _ => node.nodeType
            };
        }
    }
}
