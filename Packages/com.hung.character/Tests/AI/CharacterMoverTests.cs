using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using BrainData = Gameplay.Character.BrainSystem.BrainData;
namespace Gameplay.Character.AIBridge.Tests
{
    public class CharacterMoverTests
    {
        GameObject body;
        BrainData data;

        [SetUp]
        public void SetUp()
        {
            body = new GameObject("MoverBody");
            data = new BrainData();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(body);
        }

        [Test]
        public void Direct_WritesDirectionTowardTarget()
        {
            var mover = new CharacterMover(data, body.transform);
            Assert.IsTrue(mover.SetDestination(new Vector3(5f, 0f, 0f)));
            Assert.Greater(data.MoveDirection.x, 0f);
            Assert.AreEqual(0f, data.MoveDirection.z, 1e-4f);
            Assert.AreEqual(0f, data.MoveDirection.y, 1e-4f);
            Assert.GreaterOrEqual(data.MoveDirection.magnitude, 0.25f - 1e-4f);
            Assert.LessOrEqual(data.MoveDirection.magnitude, 1f + 1e-4f);
            Assert.IsTrue(data.HasMoveTarget);
            Assert.AreEqual(new Vector3(5f, 0f, 0f), data.MoveTarget);
            Assert.IsFalse(mover.HasArrived);
            Assert.IsFalse(data.HasArrivedAtDestination);
            Assert.AreEqual(5f, mover.RemainingDistance, 1e-4f);
        }

        [Test]
        public void Direct_ArrivesInsideArriveDistance()
        {
            body.transform.position = new Vector3(4.9f, 0f, 0f);
            var mover = new CharacterMover(data, body.transform);
            Assert.IsTrue(mover.SetDestination(new Vector3(5f, 0f, 0f)));
            Assert.IsTrue(mover.HasArrived);
            Assert.IsTrue(data.HasArrivedAtDestination);
            Assert.AreEqual(Vector3.zero, data.MoveDirection);
        }

        [Test]
        public void Direct_IgnoresHeightDifference()
        {
            var mover = new CharacterMover(data, body.transform);
            Assert.IsTrue(mover.SetDestination(new Vector3(0f, 3f, 0.1f)));
            Assert.AreEqual(0.1f, mover.RemainingDistance, 1e-4f);
            Assert.IsTrue(mover.HasArrived);
        }

        [Test]
        public void Stop_ClearsIntent()
        {
            var mover = new CharacterMover(data, body.transform);
            mover.SetDestination(new Vector3(5f, 0f, 0f));
            mover.Stop();
            Assert.AreEqual(Vector3.zero, data.MoveDirection);
            Assert.IsFalse(data.HasMoveTarget);
            Assert.IsFalse(mover.HasArrived);
            Assert.IsTrue(float.IsPositiveInfinity(mover.RemainingDistance));
        }

        [Test]
        public void OffMeshAgent_RejectsDestination_AndClearsIntent()
        {
            var agent = body.AddComponent<NavMeshAgent>();
            data.MoveDirection = Vector3.forward;
            var mover = new CharacterMover(data, body.transform, agent);
            Assert.IsFalse(mover.SetDestination(Vector3.forward * 5f));
            Assert.AreEqual(Vector3.zero, data.MoveDirection);
            Assert.IsFalse(data.HasMoveTarget);
            Assert.IsFalse(data.HasArrivedAtDestination);
            Assert.IsTrue(float.IsPositiveInfinity(data.RemainingDistance));
        }

        [Test]
        public void Steering_TurnsBodyAtTurnSpeed()
        {
            var mover = new CharacterMover(data, body.transform) { TurnSpeed = 360f };
            mover.BeginTick(0.1f);
            mover.SetDestination(new Vector3(5f, 0f, 0f));
            Assert.AreEqual(36f, body.transform.eulerAngles.y, 0.5f);
            Assert.AreEqual(0.25f, data.MoveDirection.magnitude, 1e-3f, "still facing 54 degrees away, so minimum speed rate");
        }
    }
}
