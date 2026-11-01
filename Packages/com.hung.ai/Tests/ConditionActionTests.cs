using NUnit.Framework;
namespace Hung.AI.Tests
{
    public class ConditionActionTests
    {
        [Test]
        public void IsSet_ReflectsBlackboard()
        {
            using var g = new GraphFixture();
            var alarm = g.Key("Alarm", BlackboardValueType.Bool);
            g.Add("isSet", new IsSetNode { Key = new BlackboardKeyRef(alarm.Id) });
            g.Start("isSet");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            g.SetFlag(alarm, true);
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void Compare_Numbers()
        {
            using var g = new GraphFixture();
            var ammo = g.Key("Ammo", BlackboardValueType.Int);
            ammo.DefaultValue.IntValue = 5;
            var heat = g.Key("Heat", BlackboardValueType.Float);
            heat.DefaultValue.FloatValue = 0.25f;
            g.Add("ammo", new CompareBlackboardNode { Key = new BlackboardKeyRef(ammo.Id), Op = CompareOp.Greater, Number = 3 });
            g.Add("heat", new CompareBlackboardNode { Key = new BlackboardKeyRef(heat.Id), Op = CompareOp.LessOrEqual, Number = 0.25f });
            g.Sequence("seq", "ammo", "heat");
            g.Start("seq");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void Compare_BoolAndString_Equality()
        {
            using var g = new GraphFixture();
            var alarm = g.Key("Alarm", BlackboardValueType.Bool);
            var mode = g.Key("Mode", BlackboardValueType.String);
            mode.DefaultValue.StringValue = "Hunt";
            g.Add("alarm", new CompareBlackboardNode { Key = new BlackboardKeyRef(alarm.Id), Op = CompareOp.Equal, Flag = false });
            g.Add("mode", new CompareBlackboardNode { Key = new BlackboardKeyRef(mode.Id), Op = CompareOp.NotEqual, Text = "Idle" });
            g.Sequence("seq", "alarm", "mode");
            g.Start("seq");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void Compare_OrderingOnString_FailsAndWarnsOnce()
        {
            using var g = new GraphFixture();
            var mode = g.Key("Mode", BlackboardValueType.String);
            g.Add("cmp", new CompareBlackboardNode { Key = new BlackboardKeyRef(mode.Id), Op = CompareOp.Less, Text = "x" });
            g.Start("cmp");
            using var log = new LogCounter("Only Equal and NotEqual");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, log.Count);
        }

        [Test]
        public void RandomChance_ZeroAndOne()
        {
            using var g = new GraphFixture();
            g.Add("never", new RandomChanceNode { Probability = 0f });
            g.Add("always", new RandomChanceNode { Probability = 1f });
            g.Start("never");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            g.Runtime.RootNodeId = "always";
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void Wait_SucceedsAfterDuration()
        {
            using var g = new GraphFixture();
            g.Add("wait", new WaitNode { Duration = 1f });
            g.Start("wait");
            Assert.AreEqual(NodeStatus.Running, g.Tick(0.5f));
            Assert.AreEqual(NodeStatus.Running, g.Tick(0.5f));
            Assert.AreEqual(NodeStatus.Success, g.Tick(0.5f));
        }

        [Test]
        public void SetBlackboard_WritesValue()
        {
            using var g = new GraphFixture();
            var ammo = g.Key("Ammo", BlackboardValueType.Int);
            g.Add("set", new SetBlackboardNode { Key = new BlackboardKeyRef(ammo.Id), Value = new BlackboardValue { IntValue = 7 } });
            g.Start("set");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(7, g.Blackboard.Get<int>(new BlackboardRef<int>(ammo.Id)));
        }

        [Test]
        public void SetBlackboard_MissingKey_Fails()
        {
            using var g = new GraphFixture();
            g.Add("set", new SetBlackboardNode { Key = new BlackboardKeyRef("ghost") });
            g.Start("set");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
        }

        [Test]
        public void Log_SucceedsAndLogsMessage()
        {
            using var g = new GraphFixture();
            g.Add("log", new LogNode { Message = "hello-ai-log" });
            g.Start("log");
            using var log = new LogCounter("hello-ai-log");
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, log.Count);
        }
    }
}
