using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Tests
{
    public class BlackboardTests
    {
        static BlackboardDefinition Definition(out BlackboardKey alarm, out BlackboardKey speed, out BlackboardKey target)
        {
            var def = new BlackboardDefinition();
            alarm = def.Add("Alarm", BlackboardValueType.Bool);
            speed = def.Add("Speed", BlackboardValueType.Float);
            speed.DefaultValue.FloatValue = 1.5f;
            target = def.Add("Target", BlackboardValueType.Vector3);
            return def;
        }

        [Test]
        public void Defaults_AreReadFromDefinition()
        {
            var def = Definition(out var alarm, out var speed, out _);
            var bb = new Blackboard(def);
            Assert.AreEqual(false, bb.Get<bool>(new BlackboardRef<bool>(alarm.Id)));
            Assert.AreEqual(1.5f, bb.Get<float>(new BlackboardRef<float>(speed.Id)));
            Assert.AreEqual(3, bb.Count);
            Assert.AreEqual(1, bb.IndexOfName("Speed"));
        }

        [Test]
        public void Override_ReplacesDefault_ForThisBlackboardOnly()
        {
            var def = Definition(out _, out var speed, out _);
            var overrides = new[] { new BlackboardKeyOverride { KeyId = speed.Id, Value = new BlackboardValue { FloatValue = 4f } } };
            var withOverride = new Blackboard(def, overrides);
            var plain = new Blackboard(def);
            Assert.AreEqual(4f, withOverride.Get<float>(new BlackboardRef<float>(speed.Id)));
            Assert.AreEqual(1.5f, plain.Get<float>(new BlackboardRef<float>(speed.Id)));
        }

        [Test]
        public void Set_RaisesKeyChanged_OnlyWhenValueChanges()
        {
            var def = Definition(out var alarm, out _, out _);
            var bb = new Blackboard(def);
            int raised = 0;
            string raisedId = null;
            bb.KeyChanged += id => { raised++; raisedId = id; };
            var key = new BlackboardRef<bool>(alarm.Id);
            Assert.IsTrue(bb.Set(key, true));
            Assert.IsTrue(bb.Set(key, true));
            Assert.AreEqual(1, raised);
            Assert.AreEqual(alarm.Id, raisedId);
        }

        [Test]
        public void TypeMismatch_ReturnsDefault_AndWarnsOnce()
        {
            var def = Definition(out var alarm, out _, out _);
            var bb = new Blackboard(def);
            var wrong = new BlackboardRef<float>(alarm.Id);
            using (var log = new LogCounter("was read or written as"))
            {
                Assert.AreEqual(0f, bb.Get<float>(wrong));
                Assert.AreEqual(0f, bb.Get<float>(wrong));
                Assert.IsFalse(bb.Set(wrong, 2f));
                Assert.AreEqual(1, log.Count);
            }
        }

        [Test]
        public void MissingKey_ReturnsDefault_AndWarnsOnce()
        {
            var bb = new Blackboard(Definition(out _, out _, out _));
            var ghost = new BlackboardRef<bool>("ghost-id");
            using (var log = new LogCounter("ghost-id"))
            {
                Assert.IsFalse(bb.Get<bool>(ghost));
                Assert.IsFalse(bb.Get<bool>(new BlackboardRef<bool>("ghost-id")));
                Assert.AreEqual(1, log.Count);
            }
        }

        [Test]
        public void UnassignedRef_ReadsDefault_WithoutWarning()
        {
            var bb = new Blackboard(Definition(out _, out _, out _));
            Assert.AreEqual(-1, bb.IndexOf(new BlackboardKeyRef()));
            Assert.IsFalse(bb.IsSet(new BlackboardKeyRef()));
        }

        [Test]
        public void IsSet_FollowsDocumentedRules()
        {
            var def = new BlackboardDefinition();
            var b = def.Add("B", BlackboardValueType.Bool);
            var i = def.Add("I", BlackboardValueType.Int);
            var s = def.Add("S", BlackboardValueType.String);
            var v = def.Add("V", BlackboardValueType.Vector3);
            var g = def.Add("G", BlackboardValueType.GameObject);
            var bb = new Blackboard(def);
            Assert.IsFalse(bb.IsSet(new BlackboardKeyRef(b.Id)));
            Assert.IsFalse(bb.IsSet(new BlackboardKeyRef(i.Id)));
            Assert.IsFalse(bb.IsSet(new BlackboardKeyRef(s.Id)));
            Assert.IsFalse(bb.IsSet(new BlackboardKeyRef(v.Id)));
            Assert.IsFalse(bb.IsSet(new BlackboardKeyRef(g.Id)));
            bb.Set(new BlackboardRef<bool>(b.Id), true);
            bb.Set(new BlackboardRef<int>(i.Id), 2);
            bb.Set(new BlackboardRef<string>(s.Id), "x");
            bb.Set(new BlackboardRef<Vector3>(v.Id), Vector3.one);
            var go = new GameObject("IsSetProbe");
            try
            {
                bb.Set(new BlackboardRef<GameObject>(g.Id), go);
                Assert.IsTrue(bb.IsSet(new BlackboardKeyRef(b.Id)));
                Assert.IsTrue(bb.IsSet(new BlackboardKeyRef(i.Id)));
                Assert.IsTrue(bb.IsSet(new BlackboardKeyRef(s.Id)));
                Assert.IsTrue(bb.IsSet(new BlackboardKeyRef(v.Id)));
                Assert.IsTrue(bb.IsSet(new BlackboardKeyRef(g.Id)));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TryGetPosition_ReadsVector3TransformAndGameObject()
        {
            var def = new BlackboardDefinition();
            var v = def.Add("V", BlackboardValueType.Vector3);
            var t = def.Add("T", BlackboardValueType.Transform);
            var f = def.Add("F", BlackboardValueType.Float);
            var bb = new Blackboard(def);
            bb.Set(new BlackboardRef<Vector3>(v.Id), new Vector3(1, 2, 3));
            Assert.IsTrue(bb.TryGetPosition(new BlackboardKeyRef(v.Id), out var p));
            Assert.AreEqual(new Vector3(1, 2, 3), p);
            Assert.IsFalse(bb.TryGetPosition(new BlackboardKeyRef(t.Id), out _), "null Transform is not a position");
            var go = new GameObject("PositionProbe");
            try
            {
                go.transform.position = new Vector3(5, 0, 5);
                bb.Set(new BlackboardRef<Transform>(t.Id), go.transform);
                Assert.IsTrue(bb.TryGetPosition(new BlackboardKeyRef(t.Id), out p));
                Assert.AreEqual(new Vector3(5, 0, 5), p);
            }
            finally { Object.DestroyImmediate(go); }
            using (var log = new LogCounter("a position needs"))
            {
                Assert.IsFalse(bb.TryGetPosition(new BlackboardKeyRef(f.Id), out _));
                Assert.AreEqual(1, log.Count);
            }
        }

        [Test]
        public void SetFromValue_WritesByKeyType()
        {
            var def = new BlackboardDefinition();
            var i = def.Add("I", BlackboardValueType.Int);
            var bb = new Blackboard(def);
            Assert.IsTrue(bb.SetFromValue(new BlackboardKeyRef(i.Id), new BlackboardValue { IntValue = 7 }));
            Assert.AreEqual(7, bb.Get<int>(new BlackboardRef<int>(i.Id)));
        }

        [Test]
        public void ReservedScope_WarnsAndBehavesAsAgent()
        {
            var def = new BlackboardDefinition();
            var k = def.Add("Shared", BlackboardValueType.Int);
            k.Scope = BlackboardScope.Global;
            using (var log = new LogCounter("reserved"))
            {
                var bb = new Blackboard(def);
                Assert.IsTrue(bb.Set(new BlackboardRef<int>(k.Id), 3));
                Assert.AreEqual(1, log.Count);
            }
        }
    }
}
