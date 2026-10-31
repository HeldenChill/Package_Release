namespace Hung.Base
{
    /// <summary>Outcome of an explicit service-slot registration.</summary>
    public enum ServiceRegistration
    {
        /// <summary>The empty slot accepted its first owner.</summary>
        Registered = 1,
        /// <summary>The current owner replaced its previous instance.</summary>
        Replaced = 2,
        /// <summary>The exact instance was already registered.</summary>
        AlreadyRegistered = 3,
        /// <summary>A different implementation assembly was refused.</summary>
        RefusedDuplicateOwner = 4,
        /// <summary>A null reference was refused.</summary>
        RefusedNull = 5
    }
}
