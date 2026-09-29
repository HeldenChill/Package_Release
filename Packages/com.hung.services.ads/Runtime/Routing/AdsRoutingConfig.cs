using UnityEngine;

namespace Hung.Ads
{
    /// <summary>Baked ads routing configuration. Assign to AdsManager.routingConfig to enable routed mode.</summary>
    [CreateAssetMenu(menuName = "Hung/Ads/Routing Config", fileName = "AdsRoutingConfig")]
    public sealed class AdsRoutingConfig : ScriptableObject
    {
        [SerializeField]
        AdsRoutingData data = new AdsRoutingData();

        /// <summary>The baked routing data.</summary>
        public AdsRoutingData Data => data;
    }
}
