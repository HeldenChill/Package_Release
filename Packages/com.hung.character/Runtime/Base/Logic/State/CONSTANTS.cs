using UnityEngine;

namespace Gameplay.Character
{
    public static class CONSTANTS 
    {
        public const string IS_GROUNDED_ANIM_NAME = "isGrounded";
        public const string IS_IDLE_ANIM_NAME = "isIdle";
        public const string IS_RUN_ANIM_NAME = "isRun";
        public const string JUMP_ANIM_NAME = "isJump";
    }

    public enum EFFECT
    {
        NONE = -1,
        KNOCK_BACK = 0,
        STUN = 1,
        SLOW = 2,
        TIRED = 3,
    }
}