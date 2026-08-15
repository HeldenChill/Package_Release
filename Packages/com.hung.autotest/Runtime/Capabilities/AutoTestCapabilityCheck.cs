namespace Hung.AutoTest
{
    /// <summary>
    /// Result of probing whether a single runtime capability is currently available.
    /// </summary>
    public readonly struct AutoTestCapabilityCheck
    {
        public AutoTestCapabilityCheck(bool isAvailable, string diagnostic)
        {
            IsAvailable = isAvailable;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool IsAvailable { get; }
        public string Diagnostic { get; }

        public static AutoTestCapabilityCheck Available()
            => new AutoTestCapabilityCheck(true, "available");

        public static AutoTestCapabilityCheck Unavailable(string diagnostic)
            => new AutoTestCapabilityCheck(false, diagnostic);
    }
}
