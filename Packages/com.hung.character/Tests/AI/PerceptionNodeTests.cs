using Hung.AI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
namespace Gameplay.Character.AIBridge.Tests
{
    public class PerceptionNodeTests
    {
        static void SenseThen(BrainHarness h, string conditionId)
        {
            var parallel = h.Add("par", new ParallelNode());
            parallel.AddChild("sense");
            parallel.AddChild(conditionId);
            h.Add("sense", new SenseWorldNode());
            h.Start("par");
        }

        [Test]
        public void SeeTarget_WritesKeys_RemembersThenForgets()
        {
            using var h = new BrainHarness();
            BlackboardKey targetKey = h.Key("Target", BlackboardValueType.Transform);
            BlackboardKey positionKey = h.Key("LastSeen", BlackboardValueType.Vector3);
            GameObject player = h.Target("Player", new Vector3(2f, 0f, 2f), seen: true, heard: false);
            h.Add("see", new SeeTargetNode { MemorySeconds = 6f, TargetKey = new BlackboardRef<Transform>(targetKey.Id), PositionKey = new BlackboardRef<Vector3>(positionKey.Id) });
            SenseThen(h, "see");
            Assert.AreEqual(NodeStatus.Success, h.Tick(0.1f));
            Assert.AreSame(player.transform, h.Get<Transform>(targetKey));
            Assert.AreEqual(new Vector3(2f, 0f, 2f), h.Get<Vector3>(positionKey));
            h.World.SeenColliders.Clear();
            Assert.AreEqual(NodeStatus.Success, h.Tick(1f), "remembered");
            Assert.AreEqual(NodeStatus.Failure, h.Tick(10f), "forgotten after the memory window");
        }

        [Test]
        public void DestroyedTarget_IsRememberedAsPositionOnly()
        {
            using var h = new BrainHarness();
            BlackboardKey targetKey = h.Key("Target", BlackboardValueType.Transform);
            BlackboardKey positionKey = h.Key("LastSeen", BlackboardValueType.Vector3);
            GameObject player = h.Target("Player", new Vector3(2f, 0f, 2f), seen: true, heard: false);
            h.Add("see", new SeeTargetNode { TargetKey = new BlackboardRef<Transform>(targetKey.Id), PositionKey = new BlackboardRef<Vector3>(positionKey.Id) });
            SenseThen(h, "see");
            h.Tick(0.1f);
            h.World.SeenColliders.Clear();
            Object.DestroyImmediate(player);
            Assert.AreEqual(NodeStatus.Success, h.Tick(0.5f));
            Assert.IsTrue(h.Get<Transform>(targetKey) == null, "Unity-null target");
            Assert.AreEqual(new Vector3(2f, 0f, 2f), h.Get<Vector3>(positionKey));
        }

        [Test]
        public void HearSound_WritesSoundPosition()
        {
            using var h = new BrainHarness();
            BlackboardKey soundKey = h.Key("Sound", BlackboardValueType.Vector3);
            h.Target("Noise", new Vector3(-3f, 0f, 1f), seen: false, heard: true);
            h.Add("hear", new HearSoundNode { PositionKey = new BlackboardRef<Vector3>(soundKey.Id) });
            SenseThen(h, "hear");
            Assert.AreEqual(NodeStatus.Success, h.Tick(0.1f));
            Assert.AreEqual(new Vector3(-3f, 0f, 1f), h.Get<Vector3>(soundKey));
            h.World.SoundColliders.Clear();
            Assert.AreEqual(NodeStatus.Failure, h.Tick(10f));
        }

        [Test]
        public void TrackTarget_FollowsLivePosition_AndFailsWhenLost()
        {
            using var h = new BrainHarness();
            BlackboardKey positionKey = h.Key("TargetPos", BlackboardValueType.Vector3);
            GameObject player = h.Target("Player", new Vector3(1f, 0f, 0f), seen: true, heard: false);
            h.Add("track", new TrackTargetNode { PositionKey = new BlackboardRef<Vector3>(positionKey.Id) });
            h.Start("track");
            h.Module.Perception.Sense(h.World, Vector3.zero, 0f);
            Assert.AreEqual(NodeStatus.Running, h.Tick());
            Assert.AreEqual(new Vector3(1f, 0f, 0f), h.Get<Vector3>(positionKey));
            player.transform.position = new Vector3(4f, 0f, 0f);
            Assert.AreEqual(NodeStatus.Running, h.Tick());
            Assert.AreEqual(new Vector3(4f, 0f, 0f), h.Get<Vector3>(positionKey));
            h.Module.Perception.ForgetSeen();
            Assert.AreEqual(NodeStatus.Failure, h.Tick());
        }

        [Test]
        public void LoseTarget_ForgetsAndClearsKeys()
        {
            using var h = new BrainHarness();
            BlackboardKey targetKey = h.Key("Target", BlackboardValueType.Transform);
            BlackboardKey positionKey = h.Key("LastSeen", BlackboardValueType.Vector3);
            GameObject player = h.Target("Player", new Vector3(1f, 0f, 0f), seen: true, heard: true);
            h.Add("lose", new LoseTargetNode { ForgetSound = true, TargetKey = new BlackboardRef<Transform>(targetKey.Id), PositionKey = new BlackboardRef<Vector3>(positionKey.Id) });
            h.Start("lose");
            h.Module.Perception.Sense(h.World, Vector3.zero, 0f);
            h.Agent.Blackboard.Set(new BlackboardRef<Transform>(targetKey.Id), player.transform);
            h.Agent.Blackboard.Set(new BlackboardRef<Vector3>(positionKey.Id), Vector3.one);
            Assert.AreEqual(NodeStatus.Success, h.Tick());
            Assert.IsTrue(h.Module.Perception.SeenCollider == null);
            Assert.IsFalse(h.Module.Perception.RemembersSound(0.1f, 6f));
            Assert.IsTrue(h.Get<Transform>(targetKey) == null);
            Assert.AreEqual(Vector3.zero, h.Get<Vector3>(positionKey));
        }

        [Test]
        public void LookAround_StopsMover_TurnsBody_AndSucceedsAfterDuration()
        {
            using var h = new BrainHarness();
            h.Add("look", new LookAroundNode { Duration = 2f, TurnSpeed = 90f });
            h.Start("look");
            h.Data.MoveDirection = Vector3.one;
            Assert.AreEqual(NodeStatus.Running, h.Tick(0.5f));
            Assert.AreEqual(Vector3.zero, h.Data.MoveDirection);
            Assert.AreEqual(45f, Mathf.Abs(Mathf.DeltaAngle(0f, h.Owner.transform.eulerAngles.y)), 0.5f);
            Assert.AreEqual(NodeStatus.Running, h.Tick(0.5f));
            Assert.AreEqual(NodeStatus.Running, h.Tick(0.5f));
            Assert.AreEqual(NodeStatus.Running, h.Tick(0.5f));
            Assert.AreEqual(NodeStatus.Success, h.Tick(0.5f));
        }

        [Test]
        public void WithoutModule_FailsAndWarnsOnce()
        {
            var owner = new GameObject("NoModule");
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                asset.AddNode(new SeeTargetNode { Id = "see" });
                asset.RootNodeId = "see";
                var agent = owner.AddComponent<AIAgent>();
                agent.SetGraph(asset);
                Assert.IsTrue(agent.Initialize());
                using (var log = new LogCounter("needs an AIBrainModule"))
                {
                    Assert.AreEqual(NodeStatus.Failure, agent.Tick(0.1f));
                    Assert.AreEqual(NodeStatus.Failure, agent.Tick(0.1f));
                    Assert.AreEqual(1, log.Count);
                }
                agent.ReleaseRuntime();
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void SteadyStateBrainTick_DoesNotAllocate()
        {
            using var h = new BrainHarness();
            BlackboardKey target = h.Key("Target", BlackboardValueType.Transform);
            h.Target("Player", new Vector3(10f, 0f, 0f), seen: true, heard: false);
            h.Add("sense", new SenseWorldNode());
            h.Add("see", new SeeTargetNode { TargetKey = new BlackboardRef<Transform>(target.Id), AbortType = AbortType.Both });
            h.Add("move", new MoveToNode { Target = new BlackboardKeyRef(target.Id) });
            var chase = h.Add("chase", new SequenceNode());
            chase.AddChild("see");
            chase.AddChild("move");
            h.Add("idle", new WaitNode { Duration = 100000f });
            var selector = h.Add("sel", new SelectorNode());
            selector.AddChild("chase");
            selector.AddChild("idle");
            var parallel = h.Add("par", new ParallelNode());
            parallel.AddChild("sense");
            parallel.AddChild("sel");
            h.Start("par");
            for (int i = 0; i < 5; i++) h.Tick(0.016f);
            Assert.AreEqual(NodeStatus.Running, h.Tick(0.016f));
            Assert.Greater(h.Data.MoveDirection.sqrMagnitude, 0f, "chasing");
            Assert.That(() => { h.Tick(0.016f); }, Is.Not.AllocatingGCMemory());
        }
    }
}
