using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AIGraphValidatorTests
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
            Object.DestroyImmediate(asset);
        }

        static bool Has(Dictionary<string, List<string>> issues, string nodeId, string fragment)
        {
            return issues.TryGetValue(nodeId, out List<string> list) && list.Exists(s => s.Contains(fragment));
        }

        [Test]
        public void HealthyGraph_HasNoIssues()
        {
            var seq = asset.AddNode(new SequenceNode { Id = "seq" });
            asset.AddNode(new WaitNode { Id = "w" });
            seq.AddChild("w");
            asset.RootNodeId = "seq";
            Assert.AreEqual(0, AIGraphValidator.ValidateAll(asset).Count);
        }

        [Test]
        public void MissingChild_IsReported()
        {
            var seq = asset.AddNode(new SequenceNode { Id = "seq" });
            seq.AddChild("ghost");
            asset.RootNodeId = "seq";
            Assert.IsTrue(Has(AIGraphValidator.ValidateAll(asset), "seq", "Missing child 'ghost'."));
        }

        [Test]
        public void DecoratorWithoutChild_AndEmptyComposite_AreReported()
        {
            asset.AddNode(new InverterNode { Id = "inv" });
            asset.AddNode(new SelectorNode { Id = "sel" });
            asset.RootNodeId = "inv";
            Dictionary<string, List<string>> issues = AIGraphValidator.ValidateAll(asset);
            Assert.IsTrue(Has(issues, "inv", "Needs a child."));
            Assert.IsTrue(Has(issues, "sel", "Has no children."));
        }

        [Test]
        public void UnsetAndDeletedKeys_AreReported()
        {
            BlackboardKey key = asset.Blackboard.Add("Target", BlackboardValueType.Vector3);
            asset.AddNode(new MoveToNode { Id = "unset" });
            asset.AddNode(new MoveToNode { Id = "deleted", Target = new BlackboardKeyRef("gone") });
            asset.AddNode(new MoveToNode { Id = "ok", Target = new BlackboardKeyRef(key.Id) });
            var sel = asset.AddNode(new SelectorNode { Id = "sel" });
            sel.AddChild("unset");
            sel.AddChild("deleted");
            sel.AddChild("ok");
            asset.RootNodeId = "sel";
            Dictionary<string, List<string>> issues = AIGraphValidator.ValidateAll(asset);
            Assert.IsTrue(Has(issues, "unset", "Blackboard key not set."));
            Assert.IsTrue(Has(issues, "deleted", "Blackboard key 'gone' was deleted."));
            Assert.IsFalse(issues.ContainsKey("ok"));
        }

        [Test]
        public void KeyOfWrongType_IsReported()
        {
            BlackboardKey key = asset.Blackboard.Add("Alert", BlackboardValueType.Bool);
            asset.AddNode(new BoolKeyProbeNode { Id = "probe", Flag = new BlackboardRef<bool>(key.Id) });
            asset.RootNodeId = "probe";
            Assert.AreEqual(0, AIGraphValidator.ValidateAll(asset).Count, "bool key is fine");
            key.Type = BlackboardValueType.Float;
            Assert.IsTrue(Has(AIGraphValidator.ValidateAll(asset), "probe", "Blackboard key 'Alert' is Float"));
        }

        [Test]
        public void UnreachableNode_IsReported()
        {
            asset.AddNode(new WaitNode { Id = "root" });
            asset.AddNode(new WaitNode { Id = "orphan" });
            asset.RootNodeId = "root";
            Dictionary<string, List<string>> issues = AIGraphValidator.ValidateAll(asset);
            Assert.IsTrue(Has(issues, "orphan", "Not connected to the root."));
            Assert.IsFalse(issues.ContainsKey("root"));
        }
    }
}
