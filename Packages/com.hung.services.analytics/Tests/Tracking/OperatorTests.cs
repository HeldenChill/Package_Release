using System;
using Hung.Analytics.Tracking;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class OperatorTests
    {
        static readonly DateTime T0 = new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
        TrackingState _s;

        [SetUp] public void NewState() => _s = new TrackingState(new TrackingStateModel());

        [Test]
        public void Counter_IncAndReset()
        {
            var c = new Counter(_s, "c");
            Assert.AreEqual(1, c.Inc());
            Assert.AreEqual(2, c.Inc());
            c.Reset();
            Assert.AreEqual(0, c.Value);
        }

        [Test]
        public void Since_MeasuresMinutes()
        {
            var s = new Since(_s, "s");
            Assert.IsFalse(s.HasMark);
            s.Mark(T0);
            Assert.AreEqual(4, s.Minutes(T0.AddMinutes(4)), 1e-9);
            s.Clear();
            Assert.IsFalse(s.HasMark);
        }

        [Test]
        public void Since_ClockMovedBackwards_ClampsToZero()
        {
            var s = new Since(_s, "s");
            s.Mark(T0);
            Assert.AreEqual(0, s.Minutes(T0.AddMinutes(-30)));
        }

        [Test]
        public void Accum_AddsAndDetectsCrossing()
        {
            var a = new Accum(_s, "a");
            double before = a.Value;
            double after = a.Add(6);
            Assert.IsTrue(Accum.Crossed(before, after, 5));
            Assert.IsFalse(Accum.Crossed(after, a.Add(1), 5));
        }

        [Test]
        public void Pending_KeepsFieldsAndOpenerScope()
        {
            var p = new Pending(_s, "p");
            p.Open(true, ("stage", 1), ("wave", 0));
            p.Set("wave", 4);
            Assert.IsTrue(p.TryTake(out var r));
            Assert.IsTrue(r.Ftu);
            Assert.AreEqual(1, r.GetInt("stage"));
            Assert.AreEqual(4, r.GetInt("wave"));
            Assert.IsFalse(p.IsOpen);
            Assert.IsFalse(p.TryTake(out _));
        }

        [Test]
        public void Pending_OpenReplacesPreviousFields()
        {
            var p = new Pending(_s, "p");
            p.Open(false, ("old", 1));
            p.Open(false, ("stage", 2));
            p.TryTake(out var r);
            Assert.IsNull(r.Get("old"));
        }

        [Test]
        public void OncePer_FirstPerScope()
        {
            var o = new OncePer(_s, "o");
            Assert.IsTrue(o.First("1", "lava"));
            Assert.IsFalse(o.First("1", "lava"));
            Assert.IsTrue(o.First("1", "ice"));
            Assert.IsTrue(o.First("2", "lava"));
        }

        [Test]
        public void BucketSet_InclusiveUpperBounds_LastCatchesRest()
        {
            var heat = new BucketSet(new Bucket(2, "burn"), new Bucket(10, "hot"), new Bucket(60, "chill"), new Bucket(1e9, "cold"));
            Assert.AreEqual("burn", heat.Label(2));
            Assert.AreEqual("hot", heat.Label(2.01));
            Assert.AreEqual("chill", heat.Label(60));
            Assert.AreEqual("cold", heat.Label(61));
            Assert.AreEqual("cold", heat.Label(1e12));
        }

        [Test]
        public void BucketSet_Empty_ReturnsEmptyLabel()
        {
            Assert.AreEqual("", new BucketSet().Label(5));
        }
    }
}
