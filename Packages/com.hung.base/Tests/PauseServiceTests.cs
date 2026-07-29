using NUnit.Framework;
using Hung.Base;

namespace Hung.Base.Tests
{
    public sealed class PauseServiceTests
    {
        [Test]
        public void AcquireAndRelease_FinalLeaseRestoresRunningScale()
        {
            var time = new FakeTimeScale();
            var service = new PauseService(time, 1f);
            var popup = new PauseLease(PauseLeaseId.Create(PauseLeaseKind.Popup, "popup", "1"), PauseLeaseKind.Popup, "popup");
            var ads = new PauseLease(PauseLeaseId.Create(PauseLeaseKind.Ads, "ads", "1"), PauseLeaseKind.Ads, "ads");

            Assert.IsTrue(service.Acquire(popup));
            Assert.AreEqual(0f, time.Scale);
            Assert.IsTrue(service.Acquire(ads));
            Assert.AreEqual(0f, time.Scale);
            Assert.IsTrue(service.Release(ads.Id));
            Assert.AreEqual(0f, time.Scale);
            Assert.IsTrue(service.Release(popup.Id));
            Assert.AreEqual(1f, time.Scale);
        }

        [Test]
        public void DuplicateAcquire_IsIdempotent()
        {
            var service = new PauseService(new FakeTimeScale(), 1f);
            var lease = new PauseLease(PauseLeaseId.Create(PauseLeaseKind.Ads, "ads", "1"), PauseLeaseKind.Ads, "ads");

            Assert.IsTrue(service.Acquire(lease));
            Assert.IsFalse(service.Acquire(lease));
            Assert.AreEqual(1, service.ActiveLeaseCount);
        }

        private sealed class FakeTimeScale : ITimeScale
        {
            public float Scale { get; set; } = 1f;
        }
    }
}
