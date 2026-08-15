using System;

namespace Hung.AutoTest
{
    /// <summary>
    /// Runtime capabilities an AutoTest case may require before its scenario executor runs.
    /// Values are append-only and must remain stable once released.
    /// </summary>
    [Flags]
    public enum AutoTestRuntimeCapability
    {
        None = 0,
        FakeInput = 1
    }
}
