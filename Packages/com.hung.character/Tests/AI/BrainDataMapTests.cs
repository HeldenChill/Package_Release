using Hung.AI;
using NUnit.Framework;
using UnityEngine;
using BrainData = Gameplay.Character.BrainSystem.BrainData;
namespace Gameplay.Character.AIBridge.Tests
{
    public class BrainDataMapTests
    {
        [Test]
        public void PullAndPush_CopyBoundFieldsOnly()
        {
            var def = new BlackboardDefinition();
            BlackboardKey seen = def.Add("Seen", BlackboardValueType.Bool);
            BlackboardKey position = def.Add("Pos", BlackboardValueType.Vector3);
            BlackboardKey phase = def.Add("Phase", BlackboardValueType.Int);
            var blackboard = new Blackboard(def);
            var map = new BrainDataMap();
            map.Bindings.Add(new BrainDataBinding { Key = new BlackboardKeyRef(seen.Id), Field = BrainDataField.IsSeePosition, Direction = BrainDataDirection.BlackboardToBrainData });
            map.Bindings.Add(new BrainDataBinding { Key = new BlackboardKeyRef(position.Id), Field = BrainDataField.SeenEnemyPosition, Direction = BrainDataDirection.BlackboardToBrainData });
            map.Bindings.Add(new BrainDataBinding { Key = new BlackboardKeyRef(phase.Id), Field = BrainDataField.PhaseId, Direction = BrainDataDirection.BrainDataToBlackboard });
            map.Bindings.Add(new BrainDataBinding { Field = BrainDataField.IsRun, Direction = BrainDataDirection.BlackboardToBrainData });
            var data = new BrainData { IsRun = true, PhaseId = 4 };
            blackboard.Set(new BlackboardRef<bool>(seen.Id), true);
            blackboard.Set(new BlackboardRef<Vector3>(position.Id), new Vector3(1f, 2f, 3f));
            map.Pull(data, blackboard);
            map.Push(blackboard, data);
            Assert.IsTrue(data.IsSeePosition);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), data.SeenEnemyPosition);
            Assert.AreEqual(4, blackboard.Get<int>(new BlackboardRef<int>(phase.Id)));
            Assert.IsTrue(data.IsRun, "an unassigned binding is ignored");
        }

        [Test]
        public void WrongKeyType_SkipsBinding_AndWarnsOnce()
        {
            var def = new BlackboardDefinition();
            BlackboardKey speed = def.Add("Speed", BlackboardValueType.Float);
            var blackboard = new Blackboard(def);
            var map = new BrainDataMap();
            map.Bindings.Add(new BrainDataBinding { Key = new BlackboardKeyRef(speed.Id), Field = BrainDataField.IsRun, Direction = BrainDataDirection.BlackboardToBrainData });
            var data = new BrainData { IsRun = true };
            using (var log = new LogCounter("The binding is skipped."))
            {
                map.Push(blackboard, data);
                map.Push(blackboard, data);
                Assert.AreEqual(1, log.Count);
            }
            Assert.IsTrue(data.IsRun);
        }
    }
}
