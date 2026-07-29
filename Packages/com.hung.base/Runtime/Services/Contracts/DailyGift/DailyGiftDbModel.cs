using System;
using System.Collections.Generic;

namespace Hung.Base
{
    [Serializable]
    public class DailyGiftDbModel
    {
        public int year;
        public int dayOfYear;
        public int dayCount;
        public List<bool> listDailyGiftStatus;
        public int streakDay;
        public List<bool> listStreakDailyGiftStatus;
        public int rewardDayKey;
        public int cycleStartDayKey;
        public int currentSlot;
        public int lastStreakDayKey;

        public static DailyGiftDbModel Load() { return Database.Load<DailyGiftDbModel>(); }
        public void Save() { Database.Save<DailyGiftDbModel>(this); }
    }
}
