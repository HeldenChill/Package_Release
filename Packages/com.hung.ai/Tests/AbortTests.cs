using NUnit.Framework;
namespace Hung.AI.Tests
{
    public class AbortTests
    {
        [Test]
        public void Self_AbortsRunningSibling_WhenConditionTurnsFalse()
        {
            using var g = new GraphFixture();
            var alarm = g.Key("Alarm", BlackboardValueType.Bool);
            alarm.DefaultValue.BoolValue = true;
            g.Flag("cond", alarm, AbortType.Self);
            g.Action("a", NodeStatus.Running);
            g.Sequence("seq", "cond", "a");
            g.Start("seq");
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            g.SetFlag(alarm, false);
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            int abort = ScriptedAction.Journal.IndexOf("a:abort");
            int end = ScriptedAction.Journal.IndexOf("a:end:Aborted");
            Assert.GreaterOrEqual(abort, 0);
            Assert.Greater(end, abort, "OnAbort runs before OnEnd");
        }

        [Test]
        public void NoAbortFlag_KeepsRunning_WhenConditionTurnsFalse()
        {
            using var g = new GraphFixture();
            var alarm = g.Key("Alarm", BlackboardValueType.Bool);
            alarm.DefaultValue.BoolValue = true;
            g.Flag("cond", alarm);
            g.Action("a", NodeStatus.Running);
            g.Sequence("seq", "cond", "a");
            g.Start("seq");
            g.Tick();
            g.SetFlag(alarm, false);
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(0, ScriptedAction.CountOf("a:abort"));
        }

        [Test]
        public void LowerPriority_AbortsLowerBranch_AndSwitchesInSameTick()
        {
            using var g = new GraphFixture();
            var alarm = g.Key("Alarm", BlackboardValueType.Bool);
            g.Flag("cond", alarm, AbortType.LowerPriority);
            g.Action("high", NodeStatus.Running);
            g.Sequence("highSeq", "cond", "high");
            g.Action("low", NodeStatus.Running);
            g.Selector("sel", "highSeq", "low");
            g.Start("sel");
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("low:start"));
            g.SetFlag(alarm, true);
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            int lowAbort = ScriptedAction.Journal.IndexOf("low:abort");
            int lowEnd = ScriptedAction.Journal.IndexOf("low:end:Aborted");
            int highStart = ScriptedAction.Journal.IndexOf("high:start");
            Assert.GreaterOrEqual(lowAbort, 0);
            Assert.Greater(lowEnd, lowAbort);
            Assert.Greater(highStart, lowEnd);
        }

        [Test]
        public void Both_SwitchesUpWhenTrue_AndBackDownWhenFalse()
        {
            using var g = new GraphFixture();
            var alarm = g.Key("Alarm", BlackboardValueType.Bool);
            g.Flag("cond", alarm, AbortType.Both);
            g.Action("high", NodeStatus.Running);
            g.Sequence("highSeq", "cond", "high");
            g.Action("low", NodeStatus.Running);
            g.Selector("sel", "highSeq", "low");
            g.Start("sel");
            g.Tick();
            g.SetFlag(alarm, true);
            g.Tick();
            Assert.AreEqual(1, ScriptedAction.CountOf("high:start"));
            g.SetFlag(alarm, false);
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("high:abort"));
            Assert.AreEqual(2, ScriptedAction.CountOf("low:start"), "low branch restarts in the same tick");
        }

        [Test]
        public void LowerPriority_OnBareCondition_SucceedsSelector()
        {
            using var g = new GraphFixture();
            var alarm = g.Key("Alarm", BlackboardValueType.Bool);
            g.Flag("cond", alarm, AbortType.LowerPriority);
            g.Action("low", NodeStatus.Running);
            g.Selector("sel", "cond", "low");
            g.Start("sel");
            g.Tick();
            g.SetFlag(alarm, true);
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, ScriptedAction.CountOf("low:abort"));
        }

        [Test]
        public void RunnerAbort_EndsActivePath_AndRestartsFromRoot()
        {
            using var g = new GraphFixture();
            g.Action("a", NodeStatus.Running);
            g.Sequence("seq", "a");
            g.Start("seq");
            g.Tick();
            g.Runner.Abort();
            Assert.AreEqual(1, ScriptedAction.CountOf("a:end:Aborted"));
            g.Tick();
            Assert.AreEqual(2, ScriptedAction.CountOf("a:start"));
        }
    }
}
