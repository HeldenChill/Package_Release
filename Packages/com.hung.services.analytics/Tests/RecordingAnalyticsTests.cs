using NUnit.Framework;
using Hung.Analytics;
using Hung.Base;

namespace Hung.Analytics.Tests
{
    // RecordingAnalyticsService (Runtime/Doubles/RecordingAnalyticsService.cs, Ph6 test double)
    // records every call instead of hitting Firebase/AppsFlyer - pure, no Singleton/MonoBehaviour.
    public class RecordingAnalyticsTests
    {
        [Test]
        public void RecordingService_CapturesEventNameAndParams()
        {
            var service = new RecordingAnalyticsService();

            service.LevelTrackEvent(LEVEL_STATE.COMPLETE, 3);

            Assert.AreEqual(1, service.Events.Count);
            Assert.AreEqual(nameof(service.LevelTrackEvent), service.Events[0].Name);
            Assert.AreEqual(LEVEL_STATE.COMPLETE, service.Events[0].Args[0]);
            Assert.AreEqual(3, service.Events[0].Args[1]);
        }

        [Test]
        public void RecordingService_MultipleCalls_AppendInOrder()
        {
            var service = new RecordingAnalyticsService();

            service.Day();
            service.FireUserProps();

            Assert.AreEqual(2, service.Events.Count);
            Assert.AreEqual(nameof(service.Day), service.Events[0].Name);
            Assert.AreEqual(nameof(service.FireUserProps), service.Events[1].Name);
        }

        [Test]
        public void RecordingService_OnRevenue_ImplementsRevenueSink()
        {
            IRevenueEventSink sink = new RecordingAnalyticsService();

            sink.OnRevenue("admob", 0.05, "USD");

            var service = (RecordingAnalyticsService)sink;
            Assert.AreEqual(nameof(RecordingAnalyticsService.OnRevenue), service.Events[0].Name);
        }
    }
}
