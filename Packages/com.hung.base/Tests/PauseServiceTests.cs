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

        static PauseLease AdsLease(string nonce)
            => new PauseLease(PauseLeaseId.Create(PauseLeaseKind.Ads, "ads", nonce), PauseLeaseKind.Ads, "ads");

        [Test]
        public void FinalRelease_RestoresScaleCapturedAtFirstAcquire_NotTheConstructorScale()
        {
            var time = new FakeTimeScale();
            var service = new PauseService(time, 1f);
            time.Scale = 1.8f;

            service.Acquire(AdsLease("1"));
            Assert.AreEqual(0f, time.Scale);
            service.Release(AdsLease("1").Id);

            Assert.AreEqual(1.8f, time.Scale);
        }

        [Test]
        public void NestedLeases_KeepTheFirstCapture()
        {
            var time = new FakeTimeScale { Scale = 0.5f };
            var service = new PauseService(time, 1f);

            service.Acquire(AdsLease("1"));
            service.Acquire(AdsLease("2"));
            service.Release(AdsLease("1").Id);
            service.Release(AdsLease("2").Id);

            Assert.AreEqual(0.5f, time.Scale);
        }

        [Test]
        public void FinalRelease_KeepsAScaleSomeoneElseWroteDuringThePause()
        {
            var time = new FakeTimeScale { Scale = 1.2f };
            var service = new PauseService(time, 1f);

            service.Acquire(AdsLease("1"));
            time.Scale = 1.8f;
            service.Release(AdsLease("1").Id);

            Assert.AreEqual(1.8f, time.Scale);
        }

        [Test]
        public void DeferredResumeScale_StaysPausedAndIsAppliedOnFinalRelease()
        {
            var time = new FakeTimeScale { Scale = 0.05f };
            IPauseService service = new PauseService(time, 1f);

            Assert.IsFalse(service.TryDeferResumeScale(1.8f), "nothing to defer while running");
            service.Acquire(AdsLease("1"));
            Assert.IsTrue(service.TryDeferResumeScale(1.8f));
            Assert.AreEqual(0f, time.Scale, "a deferred scale must not unpause the world");
            service.Release(AdsLease("1").Id);

            Assert.AreEqual(1.8f, time.Scale);
        }

        [Test]
        public void PauseStartedWhileFrozen_RestoresTheFreeze()
        {
            var time = new FakeTimeScale { Scale = 0f };
            var service = new PauseService(time, 1f);

            service.Acquire(AdsLease("1"));
            service.Release(AdsLease("1").Id);

            Assert.AreEqual(0f, time.Scale, "another owner's freeze is not ours to lift");
        }

        private sealed class FakeTimeScale : ITimeScale
        {
            public float Scale { get; set; } = 1f;
        }
    }
}
