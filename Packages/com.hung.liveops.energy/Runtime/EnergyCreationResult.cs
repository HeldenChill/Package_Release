namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Result of <see cref="EnergyServiceFactory.CreateLocal"/>. Distinguishes a ready
    /// service from a creation failure; a failed result never carries a working service —
    /// there is no permissive/unlimited fallback (see the plan's Global Constraints).
    /// </summary>
    public readonly struct EnergyCreationResult
    {
        public bool Success { get; }

        /// <summary>Non-null only when <see cref="Success"/> is true.</summary>
        public IEnergyService Service { get; }

        /// <summary>Actionable failure reason. Non-empty only when <see cref="Success"/> is false.</summary>
        public string ErrorMessage { get; }

        private EnergyCreationResult(bool success, IEnergyService service, string errorMessage)
        {
            Success = success;
            Service = service;
            ErrorMessage = errorMessage;
        }

        public static EnergyCreationResult Ready(IEnergyService service)
        {
            return new EnergyCreationResult(true, service, null);
        }

        public static EnergyCreationResult Failed(string errorMessage)
        {
            return new EnergyCreationResult(false, null, errorMessage);
        }
    }
}
