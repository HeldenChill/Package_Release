using System;
using Hung.Base;

namespace Hung.LiveOps.Energy.Tests
{
    /// <summary>
    /// Test-only settable clock. Later reconciliation tests (Task 6+) depend on this
    /// exact type and its members existing.
    /// </summary>
    internal sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; }

        public FakeClock(DateTime initialUtcNow)
        {
            UtcNow = initialUtcNow;
        }

        public void Advance(TimeSpan delta)
        {
            UtcNow = UtcNow + delta;
        }
    }
}
