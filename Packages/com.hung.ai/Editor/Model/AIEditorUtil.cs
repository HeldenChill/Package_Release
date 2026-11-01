using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Small helpers shared by the AI editor.</summary>
    public static class AIEditorUtil
    {
        public const string StyleSheetPath = "Packages/com.hung.ai/Editor/Styles/AIGraph.uss";
        public const string FontsFolder = "Packages/com.hung.ai/Editor/Fonts/";
        public static readonly string[] FontFiles = { "Archivo-Medium.ttf", "Archivo-Bold.ttf", "IBMPlexSans-Regular.ttf", "IBMPlexSans-Medium.ttf", "IBMPlexSans-SemiBold.ttf", "JetBrainsMono-Regular.ttf", "JetBrainsMono-Medium.ttf" };

        public static StyleSheet LoadStyleSheet() => AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);

        public static Font LoadFont(string fileName) => AssetDatabase.LoadAssetAtPath<Font>(FontsFolder + fileName);

        /// <summary>The graph whose blackboard the key fields of a serialized object point into.</summary>
        public static AIGraphAsset GraphOf(Object target)
        {
            if (target is AIGraphAsset graph) return graph;
            if (target is Component component)
            {
                AIAgent agent = component.GetComponent<AIAgent>();
                return agent != null ? agent.Graph : null;
            }
            return null;
        }

        public static int IndexOfNode(AIGraphAsset asset, string nodeId)
        {
            if (asset == null || string.IsNullOrEmpty(nodeId)) return -1;
            for (int i = 0; i < asset.Nodes.Count; i++)
                if (asset.Nodes[i] != null && asset.Nodes[i].Id == nodeId) return i;
            return -1;
        }

        /// <summary>Returns wanted, or wanted plus a number, so no other key has that name.</summary>
        public static string UniqueKeyName(BlackboardDefinition definition, string wanted, string exceptKeyId = null)
        {
            string name = wanted;
            int suffix = 1;
            while (true)
            {
                BlackboardKey existing = definition.FindByName(name);
                if (existing == null || existing.Id == exceptKeyId) return name;
                name = wanted + suffix++;
            }
        }
    }
}
