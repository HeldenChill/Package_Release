using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AIGraphClipboardTests
    {
        [Test]
        public void CopyPaste_NewIds_KeepsInternalLinksAndValues_DropsExternalLinks()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                var seq = asset.AddNode(new SequenceNode { Id = "seq" });
                asset.AddNode(new WaitNode { Id = "wait", Duration = 3f, EditorPosition = new Vector2(0f, 100f) });
                asset.AddNode(new WaitNode { Id = "outside" });
                seq.AddChild("wait");
                seq.AddChild("outside");
                asset.RootNodeId = "seq";
                string text = AIGraphClipboard.Serialize(asset, new[] { "seq", "wait" });
                Assert.IsTrue(AIGraphClipboard.CanPaste(text));
                List<AINode> pasted = AIGraphClipboard.Paste(asset, text, new Vector2(40f, 40f));
                Assert.AreEqual(2, pasted.Count);
                Assert.AreEqual(5, asset.Nodes.Count);
                var newSeq = (SequenceNode)pasted.Find(n => n is SequenceNode);
                var newWait = (WaitNode)pasted.Find(n => n is WaitNode);
                Assert.AreNotEqual("seq", newSeq.Id);
                Assert.AreNotEqual("wait", newWait.Id);
                CollectionAssert.AreEqual(new[] { newWait.Id }, newSeq.ChildIds);
                Assert.AreEqual(3f, newWait.Duration);
                Assert.AreEqual(new Vector2(40f, 40f), newSeq.EditorPosition);
                Assert.AreEqual(new Vector2(40f, 140f), newWait.EditorPosition);
                CollectionAssert.AreEqual(new[] { "wait", "outside" }, seq.ChildIds, "original is untouched");
                Assert.AreEqual("seq", asset.RootNodeId, "existing root is kept");
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [Test]
        public void CanPaste_RejectsForeignText()
        {
            Assert.IsFalse(AIGraphClipboard.CanPaste(null));
            Assert.IsFalse(AIGraphClipboard.CanPaste(string.Empty));
            Assert.IsFalse(AIGraphClipboard.CanPaste("{\"Nodes\":[]}"));
        }

        [Test]
        public void Paste_SkipsUnknownTypes_AndPastesTheRest()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                string wait = typeof(WaitNode).AssemblyQualifiedName;
                string text = AIGraphClipboard.Prefix + "{\"Nodes\":[{\"Type\":\"Nope.MissingNode, Nope\",\"Json\":\"{}\"},{\"Type\":\"" + wait + "\",\"Json\":\"{\\\"duration\\\":2.0}\"}]}";
                List<AINode> pasted = AIGraphClipboard.Paste(asset, text, Vector2.zero);
                Assert.AreEqual(1, pasted.Count);
                Assert.AreEqual(1, asset.Nodes.Count);
                Assert.AreEqual(2f, ((WaitNode)pasted[0]).Duration);
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [Test]
        public void Paste_IntoOtherGraph_RebindsKeysByName_ClearsUnmatched()
        {
            var source = ScriptableObject.CreateInstance<AIGraphAsset>();
            var target = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                BlackboardKey sourceGoal = source.Blackboard.Add("Goal", BlackboardValueType.Vector3);
                BlackboardKey sourceAlert = source.Blackboard.Add("Alert", BlackboardValueType.Bool);
                BlackboardKey sourceOnlyHere = source.Blackboard.Add("OnlyHere", BlackboardValueType.Vector3);
                source.AddNode(new MoveToNode { Id = "move", Target = new BlackboardKeyRef(sourceGoal.Id) });
                source.AddNode(new BoolKeyProbeNode { Id = "probe", Flag = new BlackboardRef<bool>(sourceAlert.Id) });
                source.AddNode(new MoveToNode { Id = "lost", Target = new BlackboardKeyRef(sourceOnlyHere.Id) });
                BlackboardKey targetGoal = target.Blackboard.Add("Goal", BlackboardValueType.Vector3);
                target.Blackboard.Add("Alert", BlackboardValueType.Float);
                string text = AIGraphClipboard.Serialize(source, new[] { "move", "probe", "lost" });
                List<AINode> pasted = AIGraphClipboard.Paste(target, text, Vector2.zero);
                Assert.AreEqual(3, pasted.Count);
                var move = (MoveToNode)pasted[0];
                var probe = (BoolKeyProbeNode)pasted[1];
                var lost = (MoveToNode)pasted[2];
                Assert.AreEqual(targetGoal.Id, move.Target.KeyId, "same name and type: rebound");
                Assert.IsFalse(probe.Flag.IsAssigned, "same name but incompatible type: cleared");
                Assert.IsFalse(lost.Target.IsAssigned, "no key with that name: cleared");
                Assert.AreEqual(sourceGoal.Id, ((MoveToNode)source.GetNode("move")).Target.KeyId, "source untouched");
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Paste_IntoSameGraph_KeepsKeyIds()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                BlackboardKey goal = asset.Blackboard.Add("Goal", BlackboardValueType.Vector3);
                asset.AddNode(new MoveToNode { Id = "move", Target = new BlackboardKeyRef(goal.Id) });
                List<AINode> pasted = AIGraphClipboard.Paste(asset, AIGraphClipboard.Serialize(asset, new[] { "move" }), Vector2.zero);
                Assert.AreEqual(goal.Id, ((MoveToNode)pasted[0]).Target.KeyId);
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [Test]
        public void Paste_IntoEmptyGraph_SetsTopPastedNodeAsRoot()
        {
            var source = ScriptableObject.CreateInstance<AIGraphAsset>();
            var target = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                source.AddNode(new WaitNode { Id = "child" });
                var sel = source.AddNode(new SelectorNode { Id = "sel" });
                sel.AddChild("child");
                string text = AIGraphClipboard.Serialize(source, new[] { "child", "sel" });
                List<AINode> pasted = AIGraphClipboard.Paste(target, text, Vector2.zero);
                Assert.AreEqual(2, pasted.Count);
                Assert.IsInstanceOf<SelectorNode>(target.Root);
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
        }
    }
}
