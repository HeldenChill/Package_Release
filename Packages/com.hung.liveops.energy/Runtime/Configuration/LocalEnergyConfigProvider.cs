using System;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Reads Energy configuration from an <see cref="EnergyConfigSO"/> asset. A null asset
    /// or any field failing <see cref="EnergyConfig"/> validation is reported as a failure
    /// result — never a permissive fallback.
    /// </summary>
    public sealed class LocalEnergyConfigProvider : IEnergyConfigProvider
    {
        private readonly EnergyConfigSO _source;

        public LocalEnergyConfigProvider(EnergyConfigSO source)
        {
            _source = source;
        }

        public EnergyConfigResult GetConfig()
        {
            if (_source == null)
            {
                return EnergyConfigResult.Failure("EnergyConfigSO reference is missing (null).");
            }

            try
            {
                EnergyConfig config = new EnergyConfig(
                    _source.renewableMax,
                    TimeSpan.FromSeconds(_source.regenerationIntervalSeconds),
                    _source.runCost,
                    _source.initialRenewable,
                    _source.initialBonus,
                    _source.transactionRetentionCapacity);

                return EnergyConfigResult.Ok(config, config.ComputeVersion());
            }
            catch (ArgumentException ex)
            {
                return EnergyConfigResult.Failure(ex.Message);
            }
        }
    }
}
