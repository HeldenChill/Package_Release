using System.Collections.Generic;
using NUnit.Framework;
namespace Hung.AI.Tests
{
    public class CompositeTests
    {
        [Test]
        public void Sequence_RunsChildrenInOrder_AndSucceeds()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Action("b", NodeStatus.Success);
            g.Sequence("seq", "a", "b");
            g.Start("seq");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            CollectionAssert.AreEqual(new[] { "a:start", "a:update", "a:end:Success", "b:start", "b:update", "b:end:Success" }, ScriptedAction.Journal);
        }

        [Test]
        public void Sequence_FailsOnFirstFailure()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Action("b", NodeStatus.Success);
            g.Sequence("seq", "a", "b");
            g.Start("seq");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(0, ScriptedAction.CountOf("b:start"));
        }

        [Test]
        public void Sequence_ResumesRunningChild_WithoutRestartingEarlierChildren()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Action("b", NodeStatus.Running);
            g.Sequence("seq", "a", "b");
            g.Start("seq");
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("a:start"));
            Assert.AreEqual(1, ScriptedAction.CountOf("b:start"));
            Assert.AreEqual(3, ScriptedAction.CountOf("b:update"));
            g.Node<ScriptedAction>("b").Result = NodeStatus.Success;
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("b:end:Success"));
            g.Tick();
            Assert.AreEqual(2, ScriptedAction.CountOf("a:start"), "root restarts after it finished");
        }

        [Test]
        public void Selector_ReturnsFirstSuccess()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Action("b", NodeStatus.Success);
            g.Action("c", NodeStatus.Success);
            g.Selector("sel", "a", "b", "c");
            g.Start("sel");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(0, ScriptedAction.CountOf("c:start"));
        }

        [Test]
        public void Selector_FailsWhenAllChildrenFail()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Action("b", NodeStatus.Failure);
            g.Selector("sel", "a", "b");
            g.Start("sel");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
        }

        [Test]
        public void Selector_ResumesRunningChild()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Action("b", NodeStatus.Running);
            g.Selector("sel", "a", "b");
            g.Start("sel");
            g.Tick();
            g.Tick();
            Assert.AreEqual(1, ScriptedAction.CountOf("a:start"));
            Assert.AreEqual(2, ScriptedAction.CountOf("b:update"));
        }

        [Test]
        public void EmptyComposites_HaveDefinedResults()
        {
            using var g = new GraphFixture();
            g.Sequence("seq");
            g.Selector("sel");
            g.Start("seq");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            g.Runtime.RootNodeId = "sel";
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
        }

        [Test]
        public void MissingChild_IsFailure_AndWarnsOnce()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Sequence("seq", "a", "ghost");
            g.Start("seq");
            using var log = new LogCounter("ghost");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, log.Count);
        }

        [Test]
        public void MissingRoot_IsFailure_AndWarnsOnce()
        {
            using var g = new GraphFixture();
            g.Start("nope");
            using var log = new LogCounter("has no root node");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, log.Count);
        }

        [Test]
        public void Cycle_DoesNotOverflow_AndRunnerRefusesToTick()
        {
            using var g = new GraphFixture();
            g.Sequence("a", "b");
            g.Sequence("b", "a");
            using var log = new LogCounter("has a cycle");
            g.Start("a");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, log.Count);
        }

        [Test]
        public void Validate_ReportsMissingRootChildAndType()
        {
            using var g = new GraphFixture();
            g.Sequence("seq", "ghost");
            g.Asset.AddNode<AINode>(null);
            g.Asset.RootNodeId = "nope";
            var errors = new List<string>();
            g.Asset.Validate(errors);
            Assert.IsTrue(errors.Exists(e => e.Contains("no root node")));
            Assert.IsTrue(errors.Exists(e => e.Contains("missing child 'ghost'")));
            Assert.IsTrue(errors.Exists(e => e.Contains("missing type")));
        }

        [Test]
        public void RuntimeCopy_IsIndependentFromAsset()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Running);
            g.Start("a");
            Assert.AreNotSame(g.Asset, g.Runtime);
            Assert.AreNotSame(g.Asset.GetNode("a"), g.Runtime.GetNode("a"));
        }
    }
}
