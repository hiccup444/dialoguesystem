using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace VNWinter.DialogueGraph.Editor
{
    [CustomEditor(typeof(DialogueGraphAsset))]
    public class DialogueGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (DialogueGraphAsset)target;

            if (GUILayout.Button("Open Graph Editor", GUILayout.Height(30)))
            {
                DialogueGraphEditorWindow.OpenWindow(asset);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Graph Summary", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Start Node:", asset.StartNode != null ? "Yes" : "No");
            EditorGUILayout.LabelField("Dialogue Nodes:", asset.DialogueNodes.Count.ToString());
            EditorGUILayout.LabelField("Choice Nodes:", asset.ChoiceNodes.Count.ToString());
            EditorGUILayout.LabelField("End Nodes:", asset.EndNodes.Count.ToString());
            EditorGUILayout.LabelField("Connections:", asset.Connections.Count.ToString());
            EditorGUI.indentLevel--;
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as DialogueGraphAsset;
            if (asset != null)
            {
                DialogueGraphEditorWindow.OpenWindow(asset);
                return true;
            }
            return false;
        }
    }
}
