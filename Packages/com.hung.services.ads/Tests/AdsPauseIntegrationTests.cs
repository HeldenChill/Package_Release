using NUnit.Framework;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public sealed class AdsPauseIntegrationTests
    {
        [Test]
        public void ReleasingAdsLease_DoesNotReleasePopupPause()
        {
            var time = new FakeTimeScale();
            var pause = new PauseService(time, 1f);
            Locator.Pause = pause;
            var popup = new PauseLease(PauseLeaseId.Create(PauseLeaseKind.Popup, "popup", "1"), PauseLeaseKind.Popup, "popup");
            pause.Acquire(popup);

            var ads = new PauseLease(PauseLeaseId.Create(PauseLeaseKind.Ads, "ads", "request-1"), PauseLeaseKind.Ads, "ads");
            pause.Acquire(ads);
            pause.Release(ads.Id);

            Assert.AreEqual(0f, time.Scale);
            Assert.IsTrue(pause.IsPaused);
        }

        private sealed class FakeTimeScale : ITimeScale
        {
            public float Scale { get; set; } = 1f;
        }
    }
}
