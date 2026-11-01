using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    [CustomEditor(typeof(AIGraphAsset))]
    public sealed class AIGraphAssetEditor : UnityEditor.Editor
    {
        VisualElement summary;
        string shownSignature;

        public override VisualElement CreateInspectorGUI()
        {
            var graph = (AIGraphAsset)target;
            var root = new VisualElement();
            root.Add(new Button(() => AIGraphEditorWindow.Open(graph)) { text = "Open Graph Editor" });
            summary = new VisualElement();
            root.Add(summary);
            shownSignature = null;
            RefreshSummary();
            // The graph is edited in the editor window, not here; poll so counts and problems stay current.
            root.schedule.Execute(RefreshSummary).Every(500);
            return root;
        }

        void RefreshSummary()
        {
            var graph = target as AIGraphAsset;
            if (graph == null || summary == null) return;
            int nodeCount = 0;
            foreach (AINode node in graph.Nodes)
                if (node != null) nodeCount++;
            var errors = new List<string>();
            graph.Validate(errors);
            string signature = nodeCount + "|" + graph.Blackboard.Keys.Count + "|" + string.Join("\n", errors);
            if (signature == shownSignature) return;
            shownSignature = signature;
            summary.Clear();
            summary.Add(new Label($"{nodeCount} nodes, {graph.Blackboard.Keys.Count} blackboard keys"));
            if (errors.Count == 0) summary.Add(new HelpBox("Graph is valid.", HelpBoxMessageType.Info));
            foreach (string error in errors) summary.Add(new HelpBox(error, HelpBoxMessageType.Warning));
        }
    }
}
