using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.DesignPattern.Tests
{
    // EventBus<T> API uses Raise, not Publish. Adjusted from the plan's assumed API
    // during Ph5 execution. Each test uses its own event struct to avoid bleeding
    // into other tests via EventBus<T>'s static per-type binding set (no Clear/Reset
    // exists to isolate tests, so [TearDown] explicitly unsubscribes instead).
    public class EventBusTests
    {
        private struct SubscribeReceivesRaiseEvent : global::Hung.DesignPattern.IEvent { public int Value; }
        private struct UnsubscribeStopsReceivingEvent : global::Hung.DesignPattern.IEvent { }
        private struct NoSubscribersEvent : global::Hung.DesignPattern.IEvent { }
        private struct TwoSubscribersEvent : global::Hung.DesignPattern.IEvent { }
        private struct SubscriberExceptionEvent : global::Hung.DesignPattern.IEvent { }

        [Test]
        public void Subscribe_ReceivesRaise()
        {
            int received = -1;
            var binding = new global::Hung.DesignPattern.EventBinding<SubscribeReceivesRaiseEvent>(e => received = e.Value);
            global::Hung.DesignPattern.EventBus<SubscribeReceivesRaiseEvent>.Subscribe(binding);

            global::Hung.DesignPattern.EventBus<SubscribeReceivesRaiseEvent>.Raise(new SubscribeReceivesRaiseEvent { Value = 42 });

            global::Hung.DesignPattern.EventBus<SubscribeReceivesRaiseEvent>.Unsubscribe(binding);
            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_StopsReceiving()
        {
            int callCount = 0;
            var binding = new global::Hung.DesignPattern.EventBinding<UnsubscribeStopsReceivingEvent>(() => callCount++);
            global::Hung.DesignPattern.EventBus<UnsubscribeStopsReceivingEvent>.Subscribe(binding);
            global::Hung.DesignPattern.EventBus<UnsubscribeStopsReceivingEvent>.Unsubscribe(binding);

            global::Hung.DesignPattern.EventBus<UnsubscribeStopsReceivingEvent>.Raise(new UnsubscribeStopsReceivingEvent());

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Raise_NoSubscribers_NoThrow()
        {
            Assert.DoesNotThrow(() =>
                global::Hung.DesignPattern.EventBus<NoSubscribersEvent>.Raise(new NoSubscribersEvent()));
        }

        [Test]
        public void TwoSubscribers_BothReceive()
        {
            int callsA = 0, callsB = 0;
            var bindingA = new global::Hung.DesignPattern.EventBinding<TwoSubscribersEvent>(() => callsA++);
            var bindingB = new global::Hung.DesignPattern.EventBinding<TwoSubscribersEvent>(() => callsB++);
            global::Hung.DesignPattern.EventBus<TwoSubscribersEvent>.Subscribe(bindingA);
            global::Hung.DesignPattern.EventBus<TwoSubscribersEvent>.Subscribe(bindingB);

            global::Hung.DesignPattern.EventBus<TwoSubscribersEvent>.Raise(new TwoSubscribersEvent());

            global::Hung.DesignPattern.EventBus<TwoSubscribersEvent>.Unsubscribe(bindingA);
            global::Hung.DesignPattern.EventBus<TwoSubscribersEvent>.Unsubscribe(bindingB);
            Assert.AreEqual(1, callsA);
            Assert.AreEqual(1, callsB);
        }

        [Test]
        public void SubscriberException_DoesNotBlockOthers()
        {
            var throwing = new global::Hung.DesignPattern.EventBinding<SubscriberExceptionEvent>(() => throw new System.Exception("boom"));
            bool secondCalled = false;
            var second = new global::Hung.DesignPattern.EventBinding<SubscriberExceptionEvent>(() => secondCalled = true);
            global::Hung.DesignPattern.EventBus<SubscriberExceptionEvent>.Subscribe(throwing);
            global::Hung.DesignPattern.EventBus<SubscriberExceptionEvent>.Subscribe(second);

            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));
            Assert.DoesNotThrow(() =>
                global::Hung.DesignPattern.EventBus<SubscriberExceptionEvent>.Raise(new SubscriberExceptionEvent()));

            global::Hung.DesignPattern.EventBus<SubscriberExceptionEvent>.Unsubscribe(throwing);
            global::Hung.DesignPattern.EventBus<SubscriberExceptionEvent>.Unsubscribe(second);
            Assert.IsTrue(secondCalled);
        }
    }
}
