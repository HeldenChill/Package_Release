namespace Hung.LiveOps.Energy.Tests
{
    /// <summary>
    /// Test-only in-memory <see cref="IEnergyStateStore"/> fake. Later persistence-failure
    /// injection tests (Tasks 6-8) depend on this exact type and its members.
    /// </summary>
    internal sealed class InMemoryEnergyStateStore : IEnergyStateStore
    {
        private EnergyState _state;

        public bool FailNextSave { get; set; }

        /// <summary>Total number of <see cref="Save"/> invocations, including failed ones. Additive test-support field.</summary>
        public int SaveCallCount { get; private set; }

        public InMemoryEnergyStateStore(EnergyState initialState = null)
        {
            _state = initialState;
        }

        public EnergyStateLoadResult Load()
        {
            return _state == null
                ? EnergyStateLoadResult.NotFound()
                : EnergyStateLoadResult.Loaded(_state);
        }

        public bool Save(EnergyState state)
        {
            SaveCallCount++;

            if (FailNextSave)
            {
                FailNextSave = false;
                return false;
            }

            _state = state;
            return true;
        }
    }
}
