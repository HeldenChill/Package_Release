namespace Hung.Analytics.Tracking
{
    /// <summary>Maps canonical tracking identifiers to wire names without changing facts or state.</summary>
    public interface ITrackingNamingProfile
    {
        /// <summary>Returns the fixed event name before any FTU prefix is added.</summary>
        string FixedName(string eventId, bool ftu, OutputMode configuredMode);
        /// <summary>Returns the B-mode template using canonical parameter holes.</summary>
        string Template(string eventId, bool ftu, string declaredTemplate);
        /// <summary>Returns the wire key for an unconsumed canonical parameter.</summary>
        string ParameterKey(string eventId, bool ftu,
            OutputMode configuredMode, string canonicalKey);
    }
}
