using UnityEngine;

namespace Hung.Base
{
    public enum SFX_TYPE
    {
        NONE = 0,
        CLICK = 1,
        BOOSTER_CLAIM = 2,
        SPIN_TICK = 3,
        SPIN_END = 4,
        COIN = 5,
        POPUP_HIDE = 6,
        UNLOCK_BOAT = 7,
        POPUP_SHOW = 8,
        ITEM_CLAIM = 9,
        REFRESH_BOOSTER = 10,
        KNIFE_BOOSTER = 11,
        HINT_BOOSTER = 12,
        CLICK_CLOSE = 13,

        UNLOCK = 23,
        WIN = 24,
        LOSE = 25,

        SPIN_WHEEL = 50,

        RICE_DOWN = 100,
        RICE_UP = 101,
        KNIFE_CUT = 102,
        KNIFE_UP = 103,
        KNIFE_DOWN = 104,
        HIT = 105,
        POP = 106,
        WHOOSH = 107,
        PERFECT = 108,
        STAR_POP_1 = 109,
        STAR_POP_2 = 110,
        STAR_POP_3 = 111,
    }

    public enum BGM_TYPE
    {
        NONE = -1,
        WAVE = 0,
        HOME = 1,
        BACK_GROUND_1 = 2,
        BACK_GROUND_2 = 3,
    }
}
