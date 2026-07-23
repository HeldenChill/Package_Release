using UnityEngine;

namespace Hung.Base
{
    public interface IDailyRewardService 
    {
        public int GetProgress{ get; }
        public int GetLastFreeClaimTime{ get; }
        public void IncreaseProgress();
        public bool CanClaimFree{ get; }
        public void ClaimFree();
        public void ResetProgress();
    }
}
