using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AIGraphEditingTests
    {
        AIGraphAsset asset;

        [SetUp]
        public void SetUp()
        {
            asset = ScriptableObject.CreateInstance<AIGraphAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearUndo(asset);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void WindowBuild_PreservesThemeStylesheets_WithoutDuplicatingCustomSheet()
        {
            var window = ScriptableObject.CreateInstance<AIGraphEditorWindow>();
            var theme = ScriptableObject.CreateInstance<UnityEngine.UIElements.StyleSheet>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            try
            {
                window.rootVisualElement.styleSheets.Add(theme);
                typeof(AIGraphEditorWindow).GetMethod("BuildGui", flags).Invoke(window, null);
                Assert.IsTrue(window.rootVisualElement.styleSheets.Contains(theme), "Building the UI must retain Unity's editor theme and font stylesheets.");
                typeof(AIGraphEditorWindow).GetMethod("OnDisable", flags).Invoke(window, null);
                typeof(AIGraphEditorWindow).GetMethod("OnEnable", flags).Invoke(window, null);
                Assert.IsTrue(window.rootVisualElement.styleSheets.Contains(theme), "Rebuilding must retain the theme too.");
                var custom = AIEditorUtil.LoadStyleSheet();
                Assert.IsNotNull(custom);
                int customCount = 0;
                for (int i = 0; i < window.rootVisualElement.styleSheets.count; i++)
                    if (window.rootVisualElement.styleSheets[i] == custom) customCount++;
                Assert.AreEqual(1, customCount, "Rebuilding must not duplicate the AI stylesheet.");
            }
            finally
            {
                Object.DestroyImmediate(window);
                Object.DestroyImmediate(theme);
            }
        }
        T Make<T>(string id, float x = 0f) where T : AINode, new()
        {
            var node = new T { Id = id, EditorPosition = new Vector2(x, 0f) };
            asset.AddNode(node);
            return node;
        }

        CompositeNode Composite(string id) => (CompositeNode)asset.GetNode(id);

        [Test]
        public void CreateNode_FirstNodeBecomesRoot()
        {
            AINode first = AIGraphEditing.CreateNode(asset, typeof(SequenceNode), new Vector2(10f, 20f));
            AINode second = AIGraphEditing.CreateNode(asset, typeof(WaitNode), Vector2.zero);
            Assert.AreEqual(first.Id, asset.RootNodeId);
            Assert.AreNotEqual(second.Id, asset.RootNodeId);
            Assert.AreEqual(new Vector2(10f, 20f), first.EditorPosition);
            Assert.AreEqual(2, asset.Nodes.Count);
            Assert.IsNull(AIGraphEditing.CreateNode(asset, typeof(CompositeNode), Vector2.zero), "abstract types are rejected");
        }

        [Test]
        public void Connect_Composite_OrdersChildrenByX()
        {
            Make<SequenceNode>("seq");
            asset.RootNodeId = "seq";
            Make<WaitNode>("right", 300f);
            Make<WaitNode>("left", -100f);
            Assert.IsTrue(AIGraphEditing.Connect(asset, "seq", "right"));
            Assert.IsTrue(AIGraphEditing.Connect(asset, "seq", "left"));
            CollectionAssert.AreEqual(new[] { "left", "right" }, Composite("seq").ChildIds);
        }

        [Test]
        public void Connect_Decorator_ReplacesChild()
        {
            Make<InverterNode>("inv");
            Make<WaitNode>("a");
            Make<WaitNode>("b");
            Assert.IsTrue(AIGraphEditing.Connect(asset, "inv", "a"));
            Assert.IsTrue(AIGraphEditing.Connect(asset, "inv", "b"));
            Assert.AreEqual("b", ((DecoratorNode)asset.GetNode("inv")).ChildId);
        }

        [Test]
        public void Connect_MovesChildAwayFromPreviousParent()
        {
            Make<SequenceNode>("s1");
            Make<SequenceNode>("s2");
            Make<WaitNode>("a");
            AIGraphEditing.Connect(asset, "s1", "a");
            AIGraphEditing.Connect(asset, "s2", "a");
            Assert.AreEqual(0, Composite("s1").ChildIds.Count);
            Assert.AreEqual("s2", AIGraphEditing.ParentIdOf(asset, "a"));
        }

        [Test]
        public void Connect_RejectsCyclesRootLeafParentsAndUnknownIds()
        {
            Make<SequenceNode>("root");
            asset.RootNodeId = "root";
            Make<SequenceNode>("a");
            Make<SequenceNode>("b");
            Make<WaitNode>("leaf");
            Assert.IsTrue(AIGraphEditing.Connect(asset, "a", "b"));
            Assert.IsFalse(AIGraphEditing.Connect(asset, "b", "a"), "cycle");
            Assert.IsFalse(AIGraphEditing.Connect(asset, "a", "a"), "self");
            Assert.IsFalse(AIGraphEditing.Connect(asset, "a", "root"), "root cannot be a child");
            Assert.IsFalse(AIGraphEditing.Connect(asset, "leaf", "a"), "leaf has no output");
            Assert.IsFalse(AIGraphEditing.Connect(asset, "a", "ghost"), "unknown child");
        }

        [Test]
        public void Disconnect_RemovesLink()
        {
            Make<SequenceNode>("seq");
            Make<WaitNode>("a");
            Make<InverterNode>("inv");
            Make<WaitNode>("b");
            AIGraphEditing.Connect(asset, "seq", "a");
            AIGraphEditing.Connect(asset, "inv", "b");
            AIGraphEditing.Disconnect(asset, "seq", "a");
            AIGraphEditing.Disconnect(asset, "inv", "b");
            Assert.AreEqual(0, Composite("seq").ChildIds.Count);
            Assert.IsNull(((DecoratorNode)asset.GetNode("inv")).ChildId);
        }

        [Test]
        public void DeleteNodes_RemovesLinksGroupMembershipAndRoot()
        {
            Make<SequenceNode>("seq");
            asset.RootNodeId = "seq";
            Make<WaitNode>("a");
            AIGraphEditing.Connect(asset, "seq", "a");
            AIGraphEditing.CreateGroup(asset, "G", Vector2.zero, new[] { "a" });
            AIGraphEditing.DeleteNodes(asset, new[] { "a" });
            Assert.IsNull(asset.GetNode("a"));
            Assert.AreEqual(0, Composite("seq").ChildIds.Count);
            Assert.AreEqual(0, asset.Groups[0].NodeIds.Count);
            AIGraphEditing.DeleteNodes(asset, new[] { "seq" });
            Assert.IsTrue(string.IsNullOrEmpty(asset.RootNodeId));
            var errors = new List<string>();
            asset.Validate(errors);
            Assert.IsTrue(errors.Exists(e => e.Contains("no root node")));
        }

        [Test]
        public void MoveNodes_ReordersCompositeChildren()
        {
            Make<SequenceNode>("seq");
            Make<WaitNode>("a", 0f);
            Make<WaitNode>("b", 100f);
            AIGraphEditing.Connect(asset, "seq", "a");
            AIGraphEditing.Connect(asset, "seq", "b");
            AIGraphEditing.MoveNodes(asset, new Dictionary<string, Vector2> { { "a", new Vector2(500f, 0f) } });
            CollectionAssert.AreEqual(new[] { "b", "a" }, Composite("seq").ChildIds);
            Assert.AreEqual(new Vector2(500f, 0f), asset.GetNode("a").EditorPosition);
        }

        [Test]
        public void MoveNodes_KeepsOrderOfCompositeWithMissingChild()
        {
            Make<SequenceNode>("seq");
            Make<WaitNode>("a", 0f);
            Make<WaitNode>("b", 100f);
            AIGraphEditing.Connect(asset, "seq", "a");
            AIGraphEditing.Connect(asset, "seq", "b");
            Composite("seq").InsertChild(1, "missing-class-node");
            AIGraphEditing.MoveNodes(asset, new Dictionary<string, Vector2> { { "a", new Vector2(500f, 0f) } });
            CollectionAssert.AreEqual(new[] { "a", "missing-class-node", "b" }, Composite("seq").ChildIds, "order is saved data; a missing child has no position to sort by");
        }

        [Test]
        public void SetRoot_DetachesNodeFromItsParent()
        {
            Make<SequenceNode>("seq");
            Make<SelectorNode>("sel");
            AIGraphEditing.Connect(asset, "seq", "sel");
            AIGraphEditing.SetRoot(asset, "sel");
            Assert.AreEqual("sel", asset.RootNodeId);
            Assert.IsNull(AIGraphEditing.ParentIdOf(asset, "sel"));
        }

        [Test]
        public void Groups_ANodeBelongsToOneGroup()
        {
            Make<WaitNode>("a");
            AIGraphGroup one = AIGraphEditing.CreateGroup(asset, "One", Vector2.zero, new[] { "a" });
            AIGraphGroup two = AIGraphEditing.CreateGroup(asset, "Two", Vector2.zero, null);
            AIGraphEditing.AddToGroup(asset, two.Id, new[] { "a" });
            Assert.AreEqual(0, one.NodeIds.Count);
            CollectionAssert.AreEqual(new[] { "a" }, two.NodeIds);
            AIGraphEditing.SetGroupTitle(asset, two.Id, "Renamed");
            Assert.AreEqual("Renamed", two.Title);
            AIGraphEditing.RemoveFromGroup(asset, two.Id, new[] { "a" });
            Assert.AreEqual(0, two.NodeIds.Count);
            AIGraphEditing.RemoveGroup(asset, one.Id);
            Assert.AreEqual(1, asset.Groups.Count);
        }

        [Test]
        public void Notes_CreateUpdateRemove()
        {
            AIGraphNote note = AIGraphEditing.CreateNote(asset, new Rect(1f, 2f, 200f, 100f));
            AIGraphEditing.UpdateNote(asset, note.Id, "T", "C", new Rect(5f, 6f, 210f, 110f));
            Assert.AreEqual("T", note.Title);
            Assert.AreEqual("C", note.Contents);
            Assert.AreEqual(new Rect(5f, 6f, 210f, 110f), note.Position);
            AIGraphEditing.RemoveNote(asset, note.Id);
            Assert.AreEqual(0, asset.Notes.Count);
        }

        [Test]
        public void CreateNode_IsUndoable()
        {
            Undo.IncrementCurrentGroup();
            AINode node = AIGraphEditing.CreateNode(asset, typeof(WaitNode), Vector2.zero);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.AreEqual(0, asset.Nodes.Count);
            Assert.IsNull(asset.GetNode(node.Id));
            Assert.IsTrue(string.IsNullOrEmpty(asset.RootNodeId));
        }
    }
}
