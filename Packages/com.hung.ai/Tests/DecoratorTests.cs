using NUnit.Framework;
namespace Hung.AI.Tests
{
    public class DecoratorTests
    {
        [Test]
        public void Parallel_RequireOneSuccess_AbortsOthers()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Running);
            g.Action("b", NodeStatus.Success);
            g.Composite("par", new ParallelNode(), "a", "b");
            g.Start("par");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("a:abort"));
            Assert.AreEqual(1, ScriptedAction.CountOf("a:end:Aborted"));
        }

        [Test]
        public void Parallel_RequireAllSuccess_WaitsForEveryChild_AndDoesNotRetickFinished()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Action("b", NodeStatus.Running);
            g.Composite("par", new ParallelNode { SuccessPolicy = ParallelPolicy.RequireAll }, "a", "b");
            g.Start("par");
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            g.Node<ScriptedAction>("b").Result = NodeStatus.Success;
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("a:update"));
        }

        [Test]
        public void Parallel_RequireOneFailure_FailsAndAbortsOthers()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Running);
            g.Action("b", NodeStatus.Failure);
            g.Composite("par", new ParallelNode(), "a", "b");
            g.Start("par");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("a:abort"));
        }

        [Test]
        public void Parallel_Empty_Succeeds()
        {
            using var g = new GraphFixture();
            g.Composite("par", new ParallelNode());
            g.Start("par");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void RandomSelector_FindsTheSucceedingChild()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Action("b", NodeStatus.Failure);
            g.Action("c", NodeStatus.Success);
            g.Composite("rnd", new RandomSelectorNode(), "a", "b", "c");
            g.Start("rnd", seed: 42);
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("c:start"));
        }

        [Test]
        public void Inverter_FlipsResult_AndPassesRunning()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Add("inv", new InverterNode { ChildId = "a" });
            g.Start("inv");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            g.Node<ScriptedAction>("a").Result = NodeStatus.Running;
            Assert.AreEqual(NodeStatus.Running, g.Tick());
        }

        [Test]
        public void Succeeder_TurnsFailureIntoSuccess()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Add("ok", new SucceederNode { ChildId = "a" });
            g.Start("ok");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void Repeater_RunsChildCountTimes()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Add("rep", new RepeaterNode { ChildId = "a", Count = 3 });
            g.Start("rep");
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(3, ScriptedAction.CountOf("a:start"));
        }

        [Test]
        public void Repeater_StopsOnFailure()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Add("rep", new RepeaterNode { ChildId = "a", Count = 3, StopOnFailure = true });
            g.Start("rep");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
        }

        [Test]
        public void UntilSuccess_RetriesUntilChildSucceeds()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Failure);
            g.Add("until", new UntilSuccessNode { ChildId = "a" });
            g.Start("until");
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            g.Node<ScriptedAction>("a").Result = NodeStatus.Success;
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void UntilFailure_SucceedsWhenChildFails()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Add("until", new UntilFailureNode { ChildId = "a" });
            g.Start("until");
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            g.Node<ScriptedAction>("a").Result = NodeStatus.Failure;
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void Cooldown_FailsUntilTimeHasPassed()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Success);
            g.Add("cd", new CooldownNode { ChildId = "a", Seconds = 1f });
            g.Start("cd");
            Assert.AreEqual(NodeStatus.Success, g.Tick(0.1f));
            Assert.AreEqual(NodeStatus.Failure, g.Tick(0.1f));
            Assert.AreEqual(NodeStatus.Success, g.Tick(1.0f));
            Assert.AreEqual(2, ScriptedAction.CountOf("a:start"));
        }

        [Test]
        public void TimeLimit_AbortsChild_WhenTimeRunsOut()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Running);
            g.Add("limit", new TimeLimitNode { ChildId = "a", Seconds = 0.5f });
            g.Start("limit");
            Assert.AreEqual(NodeStatus.Running, g.Tick(0.1f));
            Assert.AreEqual(NodeStatus.Running, g.Tick(0.3f));
            Assert.AreEqual(NodeStatus.Failure, g.Tick(0.3f));
            Assert.AreEqual(1, ScriptedAction.CountOf("a:abort"));
        }

        [Test]
        public void Decorator_WithoutChild_FailsAndWarnsOnce()
        {
            using var g = new GraphFixture();
            g.Add("inv", new InverterNode());
            g.Start("inv");
            using var log = new LogCounter("has no child");
            g.Tick();
            g.Tick();
            Assert.AreEqual(1, log.Count);
        }
    }
}
