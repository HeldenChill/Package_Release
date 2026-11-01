using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Tests
{
    public class MovementTests
    {
        static GraphFixture MoveGraph(FakeMover mover, out BlackboardKey target, float stopDistance = 0.5f)
        {
            var g = new GraphFixture();
            target = g.Key("Target", BlackboardValueType.Vector3);
            target.DefaultValue.Vector3Value = new Vector3(4, 0, 2);
            g.Add("move", new MoveToNode { Target = new BlackboardKeyRef(target.Id), StopDistance = stopDistance });
            g.Start("move", mover);
            return g;
        }

        [Test]
        public void MoveTo_RunsUntilArrived_ThenStopsMover()
        {
            var mover = new FakeMover();
            using var g = MoveGraph(mover, out _);
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(new Vector3(4, 0, 2), mover.Destination);
            mover.Arrived = true;
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, mover.StopCalls);
        }

        [Test]
        public void MoveTo_SucceedsInsideStopDistance()
        {
            var mover = new FakeMover { Remaining = 0.3f };
            using var g = MoveGraph(mover, out _, 0.5f);
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void MoveTo_WithoutMover_FailsAndWarnsOnce()
        {
            using var g = MoveGraph(null, out _);
            using var log = new LogCounter("needs an IAIMover");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, log.Count);
        }

        [Test]
        public void MoveTo_UnreachableDestination_Fails()
        {
            var mover = new FakeMover { Accept = false };
            using var g = MoveGraph(mover, out _);
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
        }

        [Test]
        public void MoveTo_UnassignedTarget_FailsAndWarnsOnce()
        {
            var mover = new FakeMover();
            using var g = new GraphFixture();
            g.Add("move", new MoveToNode());
            g.Start("move", mover);
            using var log = new LogCounter("has no target key");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, log.Count);
        }

        [Test]
        public void MoveTo_Abort_StopsMover()
        {
            var mover = new FakeMover();
            using var g = MoveGraph(mover, out _);
            g.Tick();
            g.Runner.Abort();
            Assert.AreEqual(1, mover.StopCalls);
        }

        [Test]
        public void StopMove_StopsAndSucceeds()
        {
            var mover = new FakeMover();
            using var g = new GraphFixture();
            g.Add("stop", new StopMoveNode());
            g.Start("stop", mover);
            Assert.AreEqual(NodeStatus.Success, g.Tick());
            Assert.AreEqual(1, mover.StopCalls);
        }

        [Test]
        public void Patrol_VisitsPointsInOrder_AndFinishesWhenNotLooping()
        {
            var mover = new FakeMover();
            using var g = new GraphFixture();
            var patrol = new PatrolNode { Loop = false };
            patrol.Points.Add(new Vector3(0, 0, 0));
            patrol.Points.Add(new Vector3(5, 0, 0));
            g.Add("patrol", patrol);
            g.Start("patrol", mover);
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(new Vector3(0, 0, 0), mover.Destination);
            mover.Arrived = true;
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            mover.Arrived = false;
            Assert.AreEqual(NodeStatus.Running, g.Tick());
            Assert.AreEqual(new Vector3(5, 0, 0), mover.Destination);
            mover.Arrived = true;
            Assert.AreEqual(NodeStatus.Success, g.Tick());
        }

        [Test]
        public void Patrol_WithoutPoints_FailsAndWarnsOnce()
        {
            var mover = new FakeMover();
            using var g = new GraphFixture();
            g.Add("patrol", new PatrolNode());
            g.Start("patrol", mover);
            using var log = new LogCounter("has no points");
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(NodeStatus.Failure, g.Tick());
            Assert.AreEqual(1, log.Count);
        }
    }
}
