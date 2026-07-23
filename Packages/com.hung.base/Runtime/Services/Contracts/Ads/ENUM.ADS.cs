using UnityEngine;

namespace Hung.Base
{
    public enum ADS_TYPE
    {
        MAX = 0,
        IRON_SOURCE = 1,
    }
    public enum Placement
    {
        NONE = 0,
        IN_GAME = 1,
        DAILY_REWARD = 2,
        SPIN = 3,
        KNIFE_BOOSTER = 4,
        HINT_BOOSTER = 5,
        X2_COIN = 6,
        RESET_LEVEL = 7,
        REFILL_HEART = 8,
    }

    public enum LEVEL_STATE
    {
        START = 0,
        COMPLETE = 1,
        FAIL = 2,
    }
}
