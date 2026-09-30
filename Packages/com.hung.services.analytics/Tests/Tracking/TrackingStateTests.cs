using System;
using System.Collections.Generic;
using Hung.Analytics.Tracking;
using Hung.Base.Persistence;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class TrackingStateTests
    {
        [Test]
        public void TypedValues_RoundTrip()
        {
            var s = new TrackingState(new TrackingStateModel());
            s.SetLong("a", 42);
            s.SetDouble("b", 1.5);
            s.SetString("c", "x");
            Assert.AreEqual(42, s.GetLong("a"));
            Assert.AreEqual(1.5, s.GetDouble("b"));
            Assert.AreEqual("x", s.GetString("c"));
            Assert.AreEqual(7, s.GetLong("missing", 7));
            Assert.IsTrue(s.Dirty);
        }

        [Test]
        public void SettingSameValue_DoesNotMarkDirty()
        {
            var s = new TrackingState(new TrackingStateModel());
            s.SetLong("a", 1);
            s.ClearDirty();
            s.SetLong("a", 1);
            Assert.IsFalse(s.Dirty);
        }

        [Test]
        public void RemovePrefix_RemovesOnlyMatchingKeys()
        {
            var s = new TrackingState(new TrackingStateModel());
            s.SetLong("p.a", 1);
            s.SetLong("p.b", 2);
            s.SetLong("q.a", 3);
            s.RemovePrefix("p.");
            Assert.IsFalse(s.Has("p.a"));
            Assert.IsFalse(s.Has("p.b"));
            Assert.IsTrue(s.Has("q.a"));
        }

        [Test]
        public void InMemoryStore_SavesACopy()
        {
            var store = new InMemoryTrackingStateStore();
            var model = new TrackingStateModel();
            model.Values["k"] = "1";
            Assert.IsTrue(store.Save(model));
            model.Values["k"] = "2";
            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual("1", loaded.Values["k"]);
            Assert.AreEqual(1, store.SaveCount);
        }

        [Test]
        public void InMemoryStore_Unavailable_ReturnsFalse()
        {
            var store = new InMemoryTrackingStateStore { Available = false };
            Assert.IsFalse(store.TryLoad(out var model));
            Assert.IsNull(model);
            Assert.IsFalse(store.Save(new TrackingStateModel()));
        }

        [Test]
        public void DatabaseStore_RoundTripsThroughDatabaseFacade()
        {
            var previous = Database.ServiceFactory;
            try
            {
                var fake = new FakePersistenceService();
                Database.ServiceFactory = () => fake;
                var store = new DatabaseTrackingStateStore();
                var model = new TrackingStateModel();
                model.Values["ctx.cold_starts"] = "3";

                Assert.IsTrue(store.Save(model));
                Assert.IsTrue(store.TryLoad(out var loaded));
                Assert.AreEqual("3", loaded.Values["ctx.cold_starts"]);
                Assert.AreEqual(DatabaseTrackingStateStore.SaveKey, fake.LastKey);
            }
            finally { Database.ServiceFactory = previous; }
        }

        [Test]
        public void DatabaseStore_Throwing_ReportsUnavailable()
        {
            var previous = Database.ServiceFactory;
            try
            {
                Database.ServiceFactory = () => new FakePersistenceService { Throw = true };
                var store = new DatabaseTrackingStateStore();
                Assert.IsFalse(store.TryLoad(out _));
                Assert.IsFalse(store.Save(new TrackingStateModel()));
            }
            finally { Database.ServiceFactory = previous; }
        }

        [Test]
        public void Fact_Accessors_ReadWhatFactoriesWrote()
        {
            var g = Fact.Gacha("pet", 10, "gem", 900);
            Assert.AreEqual(FactKind.Gacha, g.Kind);
            Assert.AreEqual("pet", g.Pool);
            Assert.AreEqual(10, g.Count);
            Assert.AreEqual("gem", g.CostType);
            Assert.AreEqual(900, g.CostAmount);

            var s = Fact.StageStart(12, true);
            Assert.AreEqual(12, s.Stage);
            Assert.IsTrue(s.Replay);

            Assert.AreEqual(StageResult.Abandon, Fact.StageEnd(StageResult.Abandon).Result);
            Assert.AreEqual(TutorialPhase.End, Fact.Tutorial("Tut_3", TutorialPhase.End, true).Phase);
        }

        sealed class FakePersistenceService : IPersistenceService
        {
            readonly Dictionary<Type, object> _data = new Dictionary<Type, object>();
            public bool Throw;
            public string LastKey;

            public SaveResult Save<T>(SaveDefinition<T> definition, T value) where T : new()
            {
                if (Throw) throw new InvalidOperationException("boom");
                _data[typeof(T)] = value;
                return new SaveResult(true);
            }

            public LoadResult<T> Load<T>(SaveDefinition<T> definition) where T : new()
            {
                if (Throw) throw new InvalidOperationException("boom");
                T value = _data.TryGetValue(typeof(T), out var v) ? (T)v : new T();
                return new LoadResult<T>(true, value, null, default, default);
            }

            public void Register<T>(SaveDefinition<T> definition) where T : new() { }

            public bool TryGetDefinition<T>(string requestedKey, out SaveDefinition<T> definition) where T : new()
            {
                LastKey = requestedKey;
                definition = null;
                return true;
            }
        }
    }
}
