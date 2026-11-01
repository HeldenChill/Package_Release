using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AIBlackboardPanelTests
    {
        [Test]
        public void FlipBool_FlipsRuntimeCopyOnly()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                BlackboardKey flag = asset.Blackboard.Add("Flag", BlackboardValueType.Bool);
                var live = new Blackboard(asset.Blackboard);
                int index = live.IndexOfName("Flag");
                Assert.IsTrue(AIBlackboardPanel.FlipBool(live, flag.Id));
                Assert.IsTrue(live.Get<bool>(index));
                Assert.IsTrue(AIBlackboardPanel.FlipBool(live, flag.Id));
                Assert.IsFalse(live.Get<bool>(index));
                AIBlackboardPanel.FlipBool(live, flag.Id);
                var fresh = new Blackboard(asset.Blackboard);
                Assert.IsFalse(fresh.Get<bool>(fresh.IndexOfName("Flag")));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void FlipBool_RejectsNonBoolAndMissingKeys()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                BlackboardKey speed = asset.Blackboard.Add("Speed", BlackboardValueType.Float);
                var live = new Blackboard(asset.Blackboard);
                Assert.IsFalse(AIBlackboardPanel.FlipBool(live, speed.Id));
                Assert.IsFalse(AIBlackboardPanel.FlipBool(live, "no-such-key"));
                Assert.IsFalse(AIBlackboardPanel.FlipBool(null, speed.Id));
                Assert.AreEqual(0f, live.Get<float>(live.IndexOfName("Speed")));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
