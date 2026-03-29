using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VNWinter.DialogueGraph;

namespace VNWinter.Editor
{
    /// <summary>
    /// Editor window for bulk replacing character portraits across all dialogue graphs.
    /// Access via VNWinter > Portrait Replacer menu.
    /// </summary>
    public class PortraitReplacerWindow : EditorWindow
    {
        // Portrait replacement fields
        private Sprite spriteToFind;
        private Sprite spriteToReplaceWith;

        // Custom position fields
        private string positionCharacterName = "";
        private float positionX = 0f;
        private float positionY = 0f;
        private float positionScale = 1f;

        // Shared
        private Vector2 scrollPosition;
        private List<ReplacementResult> lastResults = new List<ReplacementResult>();
        private bool showResults = false;
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Replace Portraits", "Set Custom Position", "Auto Fixer" };

        private struct ReplacementResult
        {
            public string graphName;
            public string graphPath;
            public int replacementCount;
        }

        [MenuItem("VNWinter/Portrait Replacer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PortraitReplacerWindow>("Portrait Replacer");
            window.minSize = new Vector2(400, 450);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Tab selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            EditorGUILayout.Space(10);

            if (selectedTab == 0)
            {
                DrawPortraitReplacementTab();
            }
            else if (selectedTab == 1)
            {
                DrawCustomPositionTab();
            }
            else
            {
                DrawAutoFixerTab();
            }

            // Results section (shared)
            DrawResultsSection();
        }

        private void DrawPortraitReplacementTab()
        {
            EditorGUILayout.LabelField("Bulk Portrait Replacer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Replace a portrait sprite across ALL dialogue graphs in the project.\n" +
                "Select the sprite to find and the sprite to replace it with.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Sprite to find
            EditorGUILayout.LabelField("Portrait to Find:", EditorStyles.label);
            spriteToFind = (Sprite)EditorGUILayout.ObjectField(spriteToFind, typeof(Sprite), false);

            EditorGUILayout.Space(5);

            // Sprite to replace with
            EditorGUILayout.LabelField("Replace With:", EditorStyles.label);
            spriteToReplaceWith = (Sprite)EditorGUILayout.ObjectField(spriteToReplaceWith, typeof(Sprite), false);

            EditorGUILayout.Space(15);

            // Preview button
            EditorGUI.BeginDisabledGroup(spriteToFind == null);
            if (GUILayout.Button("Preview Changes", GUILayout.Height(30)))
            {
                PreviewPortraitChanges();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            // Replace button
            EditorGUI.BeginDisabledGroup(spriteToFind == null);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Replace All Portraits", GUILayout.Height(35)))
            {
                string replacementText = spriteToReplaceWith != null
                    ? $"with \"{spriteToReplaceWith.name}\""
                    : "with nothing (clear portraits)";

                if (EditorUtility.DisplayDialog("Confirm Portrait Replacement",
                    $"This will replace \"{spriteToFind.name}\" {replacementText} in ALL dialogue graphs.\n\n" +
                    "This action cannot be undone (but you can use version control to revert).\n\n" +
                    "Continue?",
                    "Replace All", "Cancel"))
                {
                    ReplaceAllPortraits();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(15);
        }

        private void DrawCustomPositionTab()
        {
            EditorGUILayout.LabelField("Bulk Custom Position Setter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Set custom portrait position for a character across ALL dialogue graphs.\n" +
                "Also applies to Bonnie nodes in graphs where the character appears.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Character name
            EditorGUILayout.LabelField("Character Name (exact match):", EditorStyles.label);
            positionCharacterName = EditorGUILayout.TextField(positionCharacterName);

            EditorGUILayout.Space(10);

            // Position fields
            EditorGUILayout.LabelField("Custom Position Settings:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X:", GUILayout.Width(20));
            positionX = EditorGUILayout.FloatField(positionX);
            EditorGUILayout.LabelField("Y:", GUILayout.Width(20));
            positionY = EditorGUILayout.FloatField(positionY);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale:", GUILayout.Width(40));
            positionScale = EditorGUILayout.Slider(positionScale, 0.1f, 3f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            // Preview button
            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(positionCharacterName));
            if (GUILayout.Button("Preview Changes", GUILayout.Height(30)))
            {
                PreviewPositionChanges();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            // Apply button
            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(positionCharacterName));
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Apply Custom Position", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Confirm Position Change",
                    $"This will set custom position for \"{positionCharacterName}\" (and Bonnie in affected graphs):\n\n" +
                    $"Position: ({positionX}, {positionY})\n" +
                    $"Scale: {positionScale}\n\n" +
                    "This action cannot be undone (but you can use version control to revert).\n\n" +
                    "Continue?",
                    "Apply", "Cancel"))
                {
                    ApplyCustomPosition();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(15);
        }

        private void DrawAutoFixerTab()
        {
            EditorGUILayout.LabelField("Auto Fixer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Automatically fix common dialogue node settings across all graphs.",
                MessageType.Info);

            EditorGUILayout.Space(15);

            // Fix Narration button
            EditorGUILayout.LabelField("Fix Narration Nodes:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "For all nodes with isThought=true:\n" +
                "- Enable 'Show Portrait Panel'\n" +
                "- Enable 'Require Interaction'\n" +
                "- Disable 'Show Speaker Name'\n" +
                "- Set speaker name to 'Bonnie' if not already",
                MessageType.None);

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Button("Fix Narration", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Confirm Fix Narration",
                    "This will update all narration/thought nodes across ALL dialogue graphs.\n\n" +
                    "This action cannot be undone (but you can use version control to revert).\n\n" +
                    "Continue?",
                    "Fix All", "Cancel"))
                {
                    FixNarration();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(20);

            // Fix Bonnie's Dialogue button
            EditorGUILayout.LabelField("Fix Bonnie's Dialogue:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "For all nodes with speaker 'Bonnie' (non-narration):\n" +
                "- Enable 'Show Portrait Panel'\n" +
                "- Enable 'Require Interaction'\n" +
                "- Disable 'Show Speaker Name'",
                MessageType.None);

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(1f, 0.8f, 0.6f);
            if (GUILayout.Button("Fix Bonnie's Dialogue", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Confirm Fix Bonnie's Dialogue",
                    "This will update all Bonnie dialogue nodes (non-narration) across ALL dialogue graphs.\n\n" +
                    "This action cannot be undone (but you can use version control to revert).\n\n" +
                    "Continue?",
                    "Fix All", "Cancel"))
                {
                    FixBonnieDialogue();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(15);
        }

        private void DrawResultsSection()
        {
            if (showResults && lastResults.Count > 0)
            {
                EditorGUILayout.LabelField("Results:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(120));

                int totalReplacements = 0;
                foreach (var result in lastResults)
                {
                    if (result.replacementCount > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{result.graphName}: {result.replacementCount} node(s)", EditorStyles.label);
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(result.graphPath);
                            if (asset != null)
                            {
                                Selection.activeObject = asset;
                                EditorGUIUtility.PingObject(asset);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        totalReplacements += result.replacementCount;
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Total: {totalReplacements} node(s) across {lastResults.FindAll(r => r.replacementCount > 0).Count} graph(s)", EditorStyles.boldLabel);
            }
            else if (showResults)
            {
                EditorGUILayout.HelpBox("No matching dialogue nodes found.", MessageType.Warning);
            }
        }

        #region Portrait Replacement

        private void PreviewPortraitChanges()
        {
            lastResults.Clear();
            showResults = true;

            string[] guids = AssetDatabase.FindAssets("t:DialogueGraphAsset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueGraphAsset graph = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);

                if (graph == null) continue;

                int count = 0;
                foreach (var node in graph.DialogueNodes)
                {
                    if (node.portrait == spriteToFind)
                    {
                        count++;
                    }
                }

                lastResults.Add(new ReplacementResult
                {
                    graphName = graph.name,
                    graphPath = path,
                    replacementCount = count
                });
            }

            Repaint();
        }

        private void ReplaceAllPortraits()
        {
            lastResults.Clear();
            showResults = true;

            string[] guids = AssetDatabase.FindAssets("t:DialogueGraphAsset");
            int totalReplacements = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueGraphAsset graph = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);

                if (graph == null) continue;

                int count = 0;

                SerializedObject serializedGraph = new SerializedObject(graph);
                SerializedProperty dialogueNodesProperty = serializedGraph.FindProperty("dialogueNodes");

                for (int i = 0; i < dialogueNodesProperty.arraySize; i++)
                {
                    SerializedProperty nodeProperty = dialogueNodesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty portraitProperty = nodeProperty.FindPropertyRelative("portrait");

                    if (portraitProperty.objectReferenceValue == spriteToFind)
                    {
                        portraitProperty.objectReferenceValue = spriteToReplaceWith;
                        count++;
                    }
                }

                if (count > 0)
                {
                    serializedGraph.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graph);
                    totalReplacements += count;
                }

                lastResults.Add(new ReplacementResult
                {
                    graphName = graph.name,
                    graphPath = path,
                    replacementCount = count
                });
            }

            AssetDatabase.SaveAssets();

            string replacementName = spriteToReplaceWith != null ? spriteToReplaceWith.name : "null";
            Debug.Log($"Portrait Replacer: Replaced {totalReplacements} instance(s) of \"{spriteToFind.name}\" with \"{replacementName}\"");

            Repaint();
        }

        #endregion

        #region Custom Position

        private void PreviewPositionChanges()
        {
            lastResults.Clear();
            showResults = true;

            string[] guids = AssetDatabase.FindAssets("t:DialogueGraphAsset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueGraphAsset graph = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);

                if (graph == null) continue;

                // Check if this graph has the target character
                bool hasTargetCharacter = false;
                int characterCount = 0;
                int bonnieCount = 0;

                foreach (var node in graph.DialogueNodes)
                {
                    if (node.speakerName == positionCharacterName)
                    {
                        hasTargetCharacter = true;
                        characterCount++;
                    }
                }

                // If graph has target character, also count Bonnie nodes
                if (hasTargetCharacter)
                {
                    foreach (var node in graph.DialogueNodes)
                    {
                        if (string.Equals(node.speakerName?.Trim(), "Bonnie", System.StringComparison.OrdinalIgnoreCase))
                        {
                            bonnieCount++;
                        }
                    }
                }

                int totalCount = characterCount + bonnieCount;

                lastResults.Add(new ReplacementResult
                {
                    graphName = graph.name,
                    graphPath = path,
                    replacementCount = totalCount
                });
            }

            Repaint();
        }

        private void ApplyCustomPosition()
        {
            lastResults.Clear();
            showResults = true;

            string[] guids = AssetDatabase.FindAssets("t:DialogueGraphAsset");
            int totalChanges = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueGraphAsset graph = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);

                if (graph == null) continue;

                // First pass: check if this graph has the target character
                bool hasTargetCharacter = false;
                foreach (var node in graph.DialogueNodes)
                {
                    if (node.speakerName == positionCharacterName)
                    {
                        hasTargetCharacter = true;
                        break;
                    }
                }

                if (!hasTargetCharacter)
                {
                    lastResults.Add(new ReplacementResult
                    {
                        graphName = graph.name,
                        graphPath = path,
                        replacementCount = 0
                    });
                    continue;
                }

                int count = 0;

                SerializedObject serializedGraph = new SerializedObject(graph);
                SerializedProperty dialogueNodesProperty = serializedGraph.FindProperty("dialogueNodes");

                for (int i = 0; i < dialogueNodesProperty.arraySize; i++)
                {
                    SerializedProperty nodeProperty = dialogueNodesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty speakerProperty = nodeProperty.FindPropertyRelative("speakerName");

                    string speakerName = speakerProperty.stringValue?.Trim() ?? "";
                    bool isTargetCharacter = speakerName == positionCharacterName;
                    bool isBonnie = string.Equals(speakerName, "Bonnie", System.StringComparison.OrdinalIgnoreCase);

                    if (isTargetCharacter || isBonnie)
                    {
                        // Enable custom positioning
                        SerializedProperty useCustomPositioning = nodeProperty.FindPropertyRelative("useCustomPositioning");
                        SerializedProperty portraitPosition = nodeProperty.FindPropertyRelative("portraitPosition");
                        SerializedProperty portraitScale = nodeProperty.FindPropertyRelative("portraitScale");

                        useCustomPositioning.boolValue = true;
                        portraitPosition.vector2Value = new Vector2(positionX, positionY);
                        portraitScale.floatValue = positionScale;

                        count++;
                    }
                }

                if (count > 0)
                {
                    serializedGraph.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graph);
                    totalChanges += count;
                }

                lastResults.Add(new ReplacementResult
                {
                    graphName = graph.name,
                    graphPath = path,
                    replacementCount = count
                });
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"Portrait Replacer: Set custom position for {totalChanges} node(s) (character: \"{positionCharacterName}\", position: ({positionX}, {positionY}), scale: {positionScale})");

            Repaint();
        }

        #endregion

        #region Auto Fixer

        private void FixNarration()
        {
            lastResults.Clear();
            showResults = true;

            string[] guids = AssetDatabase.FindAssets("t:DialogueGraphAsset");
            int totalChanges = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueGraphAsset graph = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);

                if (graph == null) continue;

                int count = 0;

                SerializedObject serializedGraph = new SerializedObject(graph);
                SerializedProperty dialogueNodesProperty = serializedGraph.FindProperty("dialogueNodes");

                for (int i = 0; i < dialogueNodesProperty.arraySize; i++)
                {
                    SerializedProperty nodeProperty = dialogueNodesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty isThoughtProperty = nodeProperty.FindPropertyRelative("isThought");

                    if (isThoughtProperty.boolValue)
                    {
                        // Fix narration settings
                        SerializedProperty showPortraitPanel = nodeProperty.FindPropertyRelative("showPortraitPanel");
                        SerializedProperty requireInteraction = nodeProperty.FindPropertyRelative("requireInteraction");
                        SerializedProperty showSpeakerName = nodeProperty.FindPropertyRelative("showSpeakerName");
                        SerializedProperty speakerName = nodeProperty.FindPropertyRelative("speakerName");

                        showPortraitPanel.boolValue = true;
                        requireInteraction.boolValue = true;
                        showSpeakerName.boolValue = false;

                        // Set speaker name to Bonnie if not already
                        if (!string.Equals(speakerName.stringValue?.Trim(), "Bonnie", System.StringComparison.OrdinalIgnoreCase))
                        {
                            speakerName.stringValue = "Bonnie";
                        }

                        count++;
                    }
                }

                if (count > 0)
                {
                    serializedGraph.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graph);
                    totalChanges += count;
                }

                lastResults.Add(new ReplacementResult
                {
                    graphName = graph.name,
                    graphPath = path,
                    replacementCount = count
                });
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"Auto Fixer: Fixed {totalChanges} narration node(s)");

            Repaint();
        }

        private void FixBonnieDialogue()
        {
            lastResults.Clear();
            showResults = true;

            string[] guids = AssetDatabase.FindAssets("t:DialogueGraphAsset");
            int totalChanges = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueGraphAsset graph = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);

                if (graph == null) continue;

                int count = 0;

                SerializedObject serializedGraph = new SerializedObject(graph);
                SerializedProperty dialogueNodesProperty = serializedGraph.FindProperty("dialogueNodes");

                for (int i = 0; i < dialogueNodesProperty.arraySize; i++)
                {
                    SerializedProperty nodeProperty = dialogueNodesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty speakerNameProperty = nodeProperty.FindPropertyRelative("speakerName");
                    SerializedProperty isThoughtProperty = nodeProperty.FindPropertyRelative("isThought");

                    string speaker = speakerNameProperty.stringValue?.Trim() ?? "";
                    bool isBonnie = string.Equals(speaker, "Bonnie", System.StringComparison.OrdinalIgnoreCase);
                    bool isThought = isThoughtProperty.boolValue;

                    // Only fix Bonnie's non-narration dialogue
                    if (isBonnie && !isThought)
                    {
                        SerializedProperty showPortraitPanel = nodeProperty.FindPropertyRelative("showPortraitPanel");
                        SerializedProperty requireInteraction = nodeProperty.FindPropertyRelative("requireInteraction");
                        SerializedProperty showSpeakerName = nodeProperty.FindPropertyRelative("showSpeakerName");

                        showPortraitPanel.boolValue = true;
                        requireInteraction.boolValue = true;
                        showSpeakerName.boolValue = false;

                        count++;
                    }
                }

                if (count > 0)
                {
                    serializedGraph.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graph);
                    totalChanges += count;
                }

                lastResults.Add(new ReplacementResult
                {
                    graphName = graph.name,
                    graphPath = path,
                    replacementCount = count
                });
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"Auto Fixer: Fixed {totalChanges} Bonnie dialogue node(s)");

            Repaint();
        }

        #endregion
    }
}
