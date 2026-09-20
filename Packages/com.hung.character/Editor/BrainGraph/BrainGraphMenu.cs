using UnityEditor;
using UnityEngine;
using Gameplay.Character.BrainSystem;

namespace Gameplay.Character.EditorTools
{
    public static class BrainGraphMenu
    {
        [MenuItem("Assets/Create/Gameplay/Character/Default Enemy BrainGraph", priority = 101)]
        public static void CreateDefaultEnemyGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Default Enemy BrainGraph",
                "EnemyBrainGraph",
                "asset",
                "Choose a location for the default enemy BrainGraph asset.");
            if (string.IsNullOrEmpty(path)) return;

            BrainGraphAsset graph = ScriptableObject.CreateInstance<BrainGraphAsset>();
            graph.BuildDefaultEnemyGraph();
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = graph;
            BrainGraphEditorWindow.Open(graph);
        }

        [MenuItem("Tools/Character/Create Default Enemy BrainGraph")]
        public static void CreateDefaultEnemyGraphFromTools()
        {
            CreateDefaultEnemyGraph();
        }
    }
}
