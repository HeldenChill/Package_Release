using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AIGraphWindowSmokeTests
    {
        [Test]
        public void Window_LoadsNodesEdgesAndRootWithoutErrors()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            AIGraphEditorWindow window = null;
            try
            {
                var seq = asset.AddNode(new SequenceNode { Id = "seq" });
                asset.AddNode(new WaitNode { Id = "w1", EditorPosition = new Vector2(0f, 200f) });
                asset.AddNode(new WaitNode { Id = "w2", EditorPosition = new Vector2(200f, 200f) });
                seq.AddChild("w1");
                seq.AddChild("w2");
                asset.RootNodeId = "seq";
                asset.Notes.Add(new AIGraphNote { Contents = "smoke" });
                window = AIGraphEditorWindow.Open(asset);
                Assert.AreSame(asset, window.Asset);
                Assert.AreEqual(3, window.GraphView.NodeViews.Count);
                Assert.AreEqual(2, window.GraphView.edges.ToList().Count);
                Assert.IsTrue(window.GraphView.NodeViews["seq"].ClassListContains("ai-root"));
            }
            finally
            {
                if (window != null) window.Close();
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Window_LogsGraphLoadedLine()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            AIGraphEditorWindow window = null;
            try
            {
                asset.AddNode(new WaitNode { Id = "w" });
                asset.RootNodeId = "w";
                window = AIGraphEditorWindow.Open(asset);
                AIDebugLog log = window.Debugger.Log;
                Assert.Greater(log.Entries.Count, 0);
                Assert.AreEqual("Graph loaded · 1 nodes · 0 blackboard keys", log.Entries[log.Entries.Count - 1].Text);
            }
            finally
            {
                if (window != null) window.Close();
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void GraphField_And_Agent_SwitchGraph()
        {
            var a = ScriptableObject.CreateInstance<AIGraphAsset>();
            var b = ScriptableObject.CreateInstance<AIGraphAsset>();
            AIGraphEditorWindow window = null;
            GameObject withGraph = null;
            GameObject withoutGraph = null;
            try
            {
                a.AddNode(new WaitNode { Id = "wa" });
                a.RootNodeId = "wa";
                b.AddNode(new WaitNode { Id = "wb" });
                b.RootNodeId = "wb";
                window = AIGraphEditorWindow.Open(a);
                window.SelectGraph(b);
                Assert.AreSame(b, window.Asset);
                Assert.AreSame(b, window.GraphField.value);
                Assert.IsTrue(window.GraphView.NodeViews.ContainsKey("wb"));
                withGraph = new GameObject("AgentWithGraph");
                AIAgent agent = withGraph.AddComponent<AIAgent>();
                agent.SetGraph(a);
                window.SetAgent(agent);
                Assert.AreSame(a, window.Asset);
                Assert.AreSame(a, window.GraphField.value);
                withoutGraph = new GameObject("AgentWithoutGraph");
                window.SetAgent(withoutGraph.AddComponent<AIAgent>());
                Assert.AreSame(a, window.Asset);
            }
            finally
            {
                if (window != null) window.Close();
                if (withGraph != null) Object.DestroyImmediate(withGraph);
                if (withoutGraph != null) Object.DestroyImmediate(withoutGraph);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void ShouldHandleShortcut_IgnoresTextFields()
        {
            var field = new UnityEngine.UIElements.TextField();
            UnityEngine.UIElements.VisualElement inner = UnityEngine.UIElements.UQueryExtensions.Q(field, null, UnityEngine.UIElements.TextField.inputUssClassName);
            Assert.IsFalse(AIGraphEditorWindow.ShouldHandleShortcut(field));
            if (inner != null) Assert.IsFalse(AIGraphEditorWindow.ShouldHandleShortcut(inner));
            Assert.IsTrue(AIGraphEditorWindow.ShouldHandleShortcut(new UnityEngine.UIElements.VisualElement()));
            Assert.IsTrue(AIGraphEditorWindow.ShouldHandleShortcut(null));
        }
    }
}
