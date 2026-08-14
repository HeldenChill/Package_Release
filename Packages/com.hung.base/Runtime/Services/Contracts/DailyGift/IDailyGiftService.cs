using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{

    public interface IDailyGiftService
    {
        public DailyGiftDbModel DataModel { get; }
        public void InvokeCallbackDailyGift();
        public void ClaimDailyGift(int day);
        public void NextDay();
        public void ResetData();
        public bool IsInitialized();
        public bool IsClaimed(DailyGiftTrack track, int day);
        public bool CanClaim(DailyGiftTrack track, int day);
        public bool IsLocked(DailyGiftTrack track, int day);
        public List<int> GetClaimableGiftDays(DailyGiftTrack track);
        public void ClaimAllAvailableGifts(DailyGiftTrack track);
    }
}
