using UnityEditor;
using Gameplay.Character.BrainSystem;

namespace Gameplay.Character.EditorTools
{
    [CustomEditor(typeof(BrainGraphModule))]
    public sealed class BrainGraphModuleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            BrainGraphModule module = (BrainGraphModule)target;
            EditorGUILayout.Space();
            if (module.Graph != null && UnityEngine.GUILayout.Button("Open BrainGraph Editor"))
            {
                BrainGraphEditorWindow.Open(module.Graph);
            }
            if (UnityEngine.Application.isPlaying && module.DebugState != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Status", module.LastStatus.ToString());
                EditorGUILayout.LabelField("Current Node", module.DebugState.currentNodeId.ToString());
                EditorGUILayout.LabelField("Last Message", module.DebugState.lastMessage ?? string.Empty, EditorStyles.wordWrappedMiniLabel);
            }
        }
    }
}
