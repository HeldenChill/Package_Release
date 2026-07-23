using System;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    internal sealed class SystemClockTests
    {
        [Test]
        public void UtcNow_IsUtcKind()
        {
            IClock clock = new SystemClock();

            DateTime now = clock.UtcNow;

            Assert.AreEqual(DateTimeKind.Utc, now.Kind);
        }
    }
}
