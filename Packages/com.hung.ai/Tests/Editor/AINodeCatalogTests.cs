using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AINodeCatalogTests
    {
        [Test]
        public void Build_SkipsAbstract_SortsByPath_AndSetsCategory()
        {
            List<AINodeTypeInfo> list = AINodeTypeCatalog.Build(new[] { typeof(WaitNode), typeof(CompositeNode), typeof(SequenceNode), typeof(InverterNode), typeof(IsSetNode) });
            CollectionAssert.AreEqual(new[] { "Actions/Wait", "Composites/Sequence", "Conditions/Is Set", "Decorators/Inverter" }, list.ConvertAll(i => i.Path));
            Assert.AreEqual(AINodeCategory.Action, list[0].Category);
            Assert.AreEqual(AINodeCategory.Composite, list[1].Category);
            Assert.AreEqual(AINodeCategory.Condition, list[2].Category);
            Assert.AreEqual(AINodeCategory.Decorator, list[3].Category);
            Assert.AreEqual("Wait", list[0].Name);
            Assert.IsFalse(string.IsNullOrEmpty(list[0].Description));
        }

        [Test]
        public void All_ContainsCoreNodes_AndNoTestNodes()
        {
            bool foundMoveTo = false;
            foreach (AINodeTypeInfo info in AINodeTypeCatalog.All)
            {
                if (info.Type == typeof(MoveToNode)) foundMoveTo = true;
                Assert.IsFalse(info.Type.Assembly.GetName().Name.EndsWith(".Tests"), info.Type.FullName);
            }
            Assert.IsTrue(foundMoveTo);
        }

        [Test]
        public void SearchTree_HasGroupsThenEntries()
        {
            List<AINodeTypeInfo> list = AINodeTypeCatalog.Build(new[] { typeof(WaitNode), typeof(LogNode), typeof(SequenceNode) });
            List<SearchTreeEntry> tree = AINodeSearchTree.Build(list, null);
            Assert.AreEqual(6, tree.Count);
            Assert.IsInstanceOf<SearchTreeGroupEntry>(tree[0]);
            Assert.AreEqual(0, tree[0].level);
            Assert.IsInstanceOf<SearchTreeGroupEntry>(tree[1]);
            Assert.AreEqual("Actions", tree[1].name);
            Assert.AreEqual(1, tree[1].level);
            Assert.AreEqual(typeof(LogNode), tree[2].userData);
            Assert.AreEqual(2, tree[2].level);
            Assert.AreEqual(typeof(WaitNode), tree[3].userData);
            Assert.AreEqual("Composites", tree[4].name);
            Assert.AreEqual(typeof(SequenceNode), tree[5].userData);
        }

        [Test]
        public void TypeMap_FiltersKeysByRefType()
        {
            Assert.IsTrue(BlackboardTypeMap.Accepts(typeof(BlackboardRef<bool>), BlackboardValueType.Bool));
            Assert.IsFalse(BlackboardTypeMap.Accepts(typeof(BlackboardRef<bool>), BlackboardValueType.Float));
            Assert.IsTrue(BlackboardTypeMap.Accepts(typeof(BlackboardRef<int>), BlackboardValueType.Enum));
            Assert.IsTrue(BlackboardTypeMap.Accepts(typeof(BlackboardRef<Transform>), BlackboardValueType.Transform));
            Assert.IsTrue(BlackboardTypeMap.Accepts(typeof(BlackboardKeyRef), BlackboardValueType.String));
            Assert.IsNull(BlackboardTypeMap.ValueTypeOf(typeof(BlackboardKeyRef)));
            Assert.AreEqual(typeof(Vector3), BlackboardTypeMap.ValueTypeOf(typeof(BlackboardRef<Vector3>)));
        }

        [Test]
        public void MissingNodeParse_ReadsIdAndPosition()
        {
            MissingNodeInfo info = AIMissingNodes.Parse(7, "Game.OldNode", "id: abc123\ntitle: \neditorPosition: {x: 12.5, y: -3}\nchildId: zzz\n");
            Assert.AreEqual("abc123", info.NodeId);
            Assert.AreEqual(new Vector2(12.5f, -3f), info.Position);
            Assert.AreEqual("Game.OldNode", info.ClassName);
            MissingNodeInfo fallback = AIMissingNodes.Parse(9, "X", null);
            Assert.AreEqual("missing-9", fallback.NodeId);
            Assert.AreEqual(Vector2.zero, fallback.Position);
        }
    }
}
