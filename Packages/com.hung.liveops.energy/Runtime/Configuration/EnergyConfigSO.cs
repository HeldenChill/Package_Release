using UnityEngine;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Editable raw Energy configuration values, authored as a ScriptableObject asset.
    /// Validated (and turned into an immutable <see cref="EnergyConfig"/>) only by
    /// <see cref="IEnergyConfigProvider"/> implementations — this asset itself never
    /// guarantees valid values.
    /// </summary>
    [CreateAssetMenu(menuName = "Hung/LiveOps/Energy Config", fileName = "EnergyConfig")]
    public sealed class EnergyConfigSO : ScriptableObject
    {
        [Tooltip("Maximum renewable Energy. Regeneration pauses at or above this value. Bonus Energy is not capped.")]
        public int renewableMax;

        [Tooltip("Seconds required to regenerate one renewable Energy. Must be greater than zero.")]
        public float regenerationIntervalSeconds;

        [Tooltip("Energy reserved per run. Renewable Energy is spent before bonus Energy. Use 0 for free runs.")]
        public int runCost;

        [Tooltip("Renewable Energy granted when no saved Energy state exists. Must be zero or greater.")]
        public int initialRenewable;

        [Tooltip("Uncapped bonus Energy granted when no saved Energy state exists. Bonus Energy does not regenerate.")]
        public int initialBonus;

        [Tooltip("Maximum finalized transaction records retained for replay protection. Must be greater than zero.")]
        public int transactionRetentionCapacity;
    }
}
