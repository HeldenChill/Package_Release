using System;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Abstraction over wall-clock UTC time so services can be tested deterministically.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// Production clock backed by <see cref="DateTime.UtcNow"/>.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
