namespace Hung.Base
{
    /// <summary>Contract for serializable typed identifier structs backed by a stable string value.</summary>
    public interface IStableId
    {
        /// <summary>Gets the raw string value of this identifier.</summary>
        string Value { get; }

        /// <summary>Gets a value indicating whether this identifier has a valid value conforming to its grammar.</summary>
        bool IsValid { get; }
    }
}
