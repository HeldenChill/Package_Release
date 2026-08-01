using UnityEngine;

namespace Hung.Base
{
    public enum HAND_ACTION
    {
        NONE = -1,
        POINT_OUT = 0,
        MOVE_DRAG = 1,
        ZOOM_IN = 3,
        ZOOM_OUT = 4,
    }
    public enum TUTORIAL_LOADING
    {
        PREPARE_VIDEO = 0,
    }
    public enum TUTORIAL_ACTION
    {
        CLOSE_ALL = -3,
        BRANCH = -2,
        NONE = -1,
        HAND = 0,
        SHOW_VIDEO = 1,
        CALL_OUT = 2,
        END_TUTORIAL = 3,
        SHOW_CONTENT = 4,
        SHOW_CONTENT_STRING = 5,
    }
    public enum POSITION_TYPE
    {
        NONE = -1,
        WORLD_POS = 0,
        UI_POS = 1,
    }
}
