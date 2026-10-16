using Hung.AI;
using NUnit.Framework;
using UnityEngine;
namespace Gameplay.Character.AIBridge.Tests
{
    public class AIBrainModuleTests
    {
        [Test]
        public void HostTick_RunsGraph_AndMoveToWritesBrainData()
        {
            using var h = new BrainHarness();
            BlackboardKey goal = h.Key("Goal", BlackboardValueType.Vector3);
            goal.DefaultValue.Vector3Value = new Vector3(0f, 0f, 6f);
            h.Add("move", new MoveToNode { Target = new BlackboardKeyRef(goal.Id) });
            h.Start("move");
            Assert.IsFalse(h.Agent.AutoTick, "the module drives the agent");
            Assert.AreSame(h.Module.Mover, h.Agent.Mover);
            Assert.AreEqual(NodeStatus.Running, h.Tick());
            Assert.AreEqual(1f, h.Data.MoveDirection.z, 1e-3f, "facing the goal, so full speed rate");
            Assert.IsTrue(h.Data.HasMoveTarget);
            Assert.AreEqual(new Vector3(0f, 0f, 6f), h.Data.MoveTarget);
        }

        [Test]
        public void StopNavigation_PausesAndClearsMove_StartResumes()
        {
            using var h = new BrainHarness();
            BlackboardKey goal = h.Key("Goal", BlackboardValueType.Vector3);
            goal.DefaultValue.Vector3Value = new Vector3(0f, 0f, 6f);
            h.Add("move", new MoveToNode { Target = new BlackboardKeyRef(goal.Id) });
            h.Start("move");
            h.Tick();
            h.Module.StopNavigation();
            Assert.IsTrue(h.Agent.IsPaused);
            Assert.AreEqual(Vector3.zero, h.Data.MoveDirection);
            Assert.IsFalse(h.Data.HasMoveTarget);
            Assert.AreEqual(NodeStatus.None, h.Tick());
            Assert.AreEqual(Vector3.zero, h.Data.MoveDirection);
            h.Module.StartNavigation();
            Assert.IsFalse(h.Agent.IsPaused);
            Assert.AreEqual(NodeStatus.Running, h.Tick());
            Assert.Greater(h.Data.MoveDirection.z, 0f);
        }

        [Test]
        public void PausedBrain_DoesNotCopyBindings_ResumingRestoresMapping()
        {
            using var h = new BrainHarness();
            BlackboardKey run = h.Key("Run", BlackboardValueType.Bool);
            BlackboardKey phase = h.Key("Phase", BlackboardValueType.Int);
            run.DefaultValue.BoolValue = true;
            h.Module.BrainDataMap.Bindings.Add(new BrainDataBinding { Key = new BlackboardKeyRef(run.Id), Field = BrainDataField.IsRun, Direction = BrainDataDirection.BlackboardToBrainData });
            h.Module.BrainDataMap.Bindings.Add(new BrainDataBinding { Key = new BlackboardKeyRef(phase.Id), Field = BrainDataField.PhaseId, Direction = BrainDataDirection.BrainDataToBlackboard });
            h.Add("wait", new WaitNode { Duration = 100f });
            h.Start("wait");
            h.Tick();
            Assert.IsTrue(h.Data.IsRun);
            h.Module.StopNavigation();
            h.Data.IsRun = false;
            h.Data.PhaseId = 7;
            Assert.AreEqual(NodeStatus.None, h.Tick());
            Assert.IsFalse(h.Data.IsRun, "paused graph must not overwrite scripted intent");
            Assert.AreEqual(0, h.Get<int>(phase), "paused graph must not pull input either");
            h.Module.StartNavigation();
            h.Tick();
            Assert.IsTrue(h.Data.IsRun);
            Assert.AreEqual(7, h.Get<int>(phase));
        }

        [TestCase(0.1f)]
        [TestCase(0.4f)]
        public void MoveToCompletion_PreservesArrivalReadback(float distance)
        {
            using var h = new BrainHarness();
            BlackboardKey goal = h.Key("Goal", BlackboardValueType.Vector3);
            goal.DefaultValue.Vector3Value = Vector3.forward * distance;
            h.Add("move", new MoveToNode { Target = new BlackboardKeyRef(goal.Id), StopDistance = 0.5f });
            h.Start("move");
            Assert.AreEqual(NodeStatus.Success, h.Tick());
            Assert.IsTrue(h.Data.HasArrivedAtDestination);
            Assert.AreEqual(distance, h.Data.RemainingDistance, 0.001f);
            Assert.AreEqual(Vector3.zero, h.Data.MoveDirection);
            Assert.IsFalse(h.Data.HasMoveTarget);
            h.Module.StopNavigation();
            Assert.IsFalse(h.Data.HasArrivedAtDestination, "cancellation clears completion");
        }

        [Test]
        public void PatrolCompletion_PreservesArrivalReadback()
        {
            using var h = new BrainHarness();
            var patrol = h.Add("patrol", new PatrolNode { Loop = false });
            patrol.Points.Add(Vector3.forward * 0.4f);
            h.Start("patrol");
            Assert.AreEqual(NodeStatus.Success, h.Tick());
            Assert.IsTrue(h.Data.HasArrivedAtDestination);
            Assert.AreEqual(0.4f, h.Data.RemainingDistance, 0.001f);
            Assert.AreEqual(Vector3.zero, h.Data.MoveDirection);
        }

        [Test]
        public void NoGraph_DoesNotTick()
        {
            using var h = new BrainHarness();
            h.Data.MoveDirection = new Vector3(1f, 0f, 0f);
            using (var log = new LogCounter("has no graph"))
            {
                h.Module.Initialize(h.Data, h.Parameter);
                Assert.AreEqual(NodeStatus.None, h.Tick());
                Assert.AreEqual(NodeStatus.None, h.Tick());
                Assert.AreEqual(1, log.Count);
            }
            Assert.AreEqual(new Vector3(1f, 0f, 0f), h.Data.MoveDirection, "BrainData untouched");
        }
    }
}
