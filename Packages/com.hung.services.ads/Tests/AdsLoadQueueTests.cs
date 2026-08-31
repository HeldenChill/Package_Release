using NUnit.Framework;

namespace Hung.Ads.Tests
{
    public sealed class AdsLoadQueueTests
    {
        [Test]
        public void LoadQueue_AdvancesAfterLoadFailure()
        {
            var queue = new AdsLoadQueue();
            int first = 0;
            int second = 0;

            queue.Enqueue(() => first++);
            queue.Enqueue(() => second++);
            queue.MarkCurrentComplete();

            Assert.AreEqual(1, first);
            Assert.AreEqual(1, second);
        }
    }
}
