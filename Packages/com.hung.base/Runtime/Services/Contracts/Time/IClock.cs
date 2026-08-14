using System;

namespace Hung.Base
{
    /// <summary>
    /// Provides authoritative UTC time for deterministic gameplay services.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Current time in UTC. Implementations must return <see cref="DateTimeKind.Utc"/>.
        /// </summary>
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// Production clock. This is the only affected-domain type that reads wall-clock UTC directly.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
