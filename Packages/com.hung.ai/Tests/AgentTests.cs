using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
namespace Hung.AI.Tests
{
    public class AgentTests
    {
        GameObject go;
        AIGraphAsset asset;

        [SetUp]
        public void SetUp()
        {
            ScriptedAction.Journal.Clear();
            asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            go = new GameObject("AgentUnderTest");
        }

        [TearDown]
        public void TearDown()
        {
            var agent = go.GetComponent<AIAgent>();
            if (agent != null) agent.ReleaseRuntime();
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(asset);
        }

        AIAgent MakeAgent()
        {
            var agent = go.AddComponent<AIAgent>();
            agent.SetGraph(asset);
            Assert.IsTrue(agent.Initialize());
            return agent;
        }

        [Test]
        public void Initialize_UsesRuntimeCopy()
        {
            asset.AddNode(new ScriptedAction { Id = "a", Result = NodeStatus.Running });
            asset.RootNodeId = "a";
            var agent = MakeAgent();
            Assert.IsNotNull(agent.RuntimeGraph);
            Assert.AreNotSame(asset, agent.RuntimeGraph);
        }

        [Test]
        public void EditingAssetAfterInitialize_DoesNotChangeRunningAgent()
        {
            var speed = asset.Blackboard.Add("Speed", BlackboardValueType.Float);
            speed.DefaultValue.FloatValue = 1f;
            asset.AddNode(new ScriptedAction { Id = "a", Result = NodeStatus.Running });
            asset.RootNodeId = "a";
            var agent = MakeAgent();
            speed.DefaultValue.FloatValue = 9f;
            asset.RootNodeId = "missing";
            Assert.AreEqual(1f, agent.Blackboard.Get<float>(new BlackboardRef<float>(speed.Id)));
            Assert.AreEqual(NodeStatus.Running, agent.Tick(0.1f));
        }

        [Test]
        public void Override_AppliesToThisAgentOnly()
        {
            var speed = asset.Blackboard.Add("Speed", BlackboardValueType.Float);
            speed.DefaultValue.FloatValue = 1f;
            asset.AddNode(new ScriptedAction { Id = "a" });
            asset.RootNodeId = "a";
            var agent = go.AddComponent<AIAgent>();
            agent.SetGraph(asset);
            agent.Overrides.Add(new BlackboardKeyOverride { KeyId = speed.Id, Value = new BlackboardValue { FloatValue = 3f } });
            Assert.IsTrue(agent.Initialize());
            Assert.AreEqual(3f, agent.Blackboard.Get<float>(new BlackboardRef<float>(speed.Id)));
            Assert.AreEqual(1f, speed.DefaultValue.FloatValue);
        }

        [Test]
        public void Mover_IsFoundOnSameGameObject()
        {
            var mover = go.AddComponent<FakeMoverBehaviour>();
            asset.AddNode(new ScriptedAction { Id = "a" });
            asset.RootNodeId = "a";
            var agent = MakeAgent();
            Assert.IsNotNull(mover);
            Assert.AreSame(mover, agent.Mover);
        }

        [Test]
        public void Pause_AbortsRunningLeaf_StopsMover_AndResumeRestarts()
        {
            var mover = go.AddComponent<FakeMoverBehaviour>();
            asset.AddNode(new ScriptedAction { Id = "a", Result = NodeStatus.Running });
            asset.RootNodeId = "a";
            var agent = MakeAgent();
            agent.Tick(0.1f);
            agent.Pause();
            Assert.IsTrue(agent.IsPaused);
            Assert.AreEqual(1, ScriptedAction.CountOf("a:abort"));
            Assert.AreEqual(1, ScriptedAction.CountOf("a:end:Aborted"));
            Assert.AreEqual(1, mover.StopCalls);
            Assert.AreEqual(NodeStatus.None, agent.Tick(0.1f));
            Assert.AreEqual(1, ScriptedAction.CountOf("a:update"));
            agent.Resume();
            agent.Tick(0.1f);
            Assert.AreEqual(2, ScriptedAction.CountOf("a:start"));
        }

        [Test]
        public void AbortActivePath_EndsRunningLeaf_WithoutPausing()
        {
            asset.AddNode(new ScriptedAction { Id = "a", Result = NodeStatus.Running });
            asset.RootNodeId = "a";
            var agent = MakeAgent();
            agent.Tick(0.1f);
            agent.AbortActivePath();
            Assert.AreEqual(1, ScriptedAction.CountOf("a:end:Aborted"));
            Assert.IsFalse(agent.IsPaused);
        }

        [Test]
        public void NoGraph_InitializeFails_WithWarning()
        {
            var agent = go.AddComponent<AIAgent>();
            using var log = new LogCounter("has no graph");
            Assert.IsFalse(agent.Initialize());
            Assert.AreEqual(1, log.Count);
        }

        [Test]
        public void Tick_DoesNotAllocate_InSteadyStateAndWhenSwitchingBranches()
        {
            var hasTarget = asset.Blackboard.Add("HasTarget", BlackboardValueType.Bool);
            var target = asset.Blackboard.Add("Target", BlackboardValueType.Vector3);
            target.DefaultValue.Vector3Value = new Vector3(3, 0, 3);
            var sense = asset.AddNode(new WaitNode { Id = "sense", Duration = 100000f });
            var cond = asset.AddNode(new IsSetNode { Id = "cond", Key = new BlackboardKeyRef(hasTarget.Id), AbortType = AbortType.Both });
            var move = asset.AddNode(new MoveToNode { Id = "move", Target = new BlackboardKeyRef(target.Id) });
            var chase = asset.AddNode(new SequenceNode { Id = "chase" });
            chase.AddChild("cond");
            chase.AddChild("move");
            var idle = asset.AddNode(new WaitNode { Id = "idle", Duration = 100000f });
            var sel = asset.AddNode(new SelectorNode { Id = "sel" });
            sel.AddChild("chase");
            sel.AddChild("idle");
            var par = asset.AddNode(new ParallelNode { Id = "par" });
            par.AddChild("sense");
            par.AddChild("sel");
            asset.RootNodeId = "par";
            var agent = MakeAgent();
            var mover = new FakeMover();
            agent.Mover = mover;
            var hasTargetRef = new BlackboardRef<bool>(hasTarget.Id);
            for (int i = 0; i < 3; i++) agent.Tick(0.016f);
            agent.Blackboard.Set(hasTargetRef, true);
            agent.Tick(0.016f);
            agent.Blackboard.Set(hasTargetRef, false);
            agent.Tick(0.016f);
            Assert.That(() => { agent.Tick(0.016f); }, Is.Not.AllocatingGCMemory());
            Assert.That(() =>
            {
                agent.Blackboard.Set(hasTargetRef, true);
                agent.Tick(0.016f);
                agent.Blackboard.Set(hasTargetRef, false);
                agent.Tick(0.016f);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
