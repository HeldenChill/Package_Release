namespace Gameplay.Character.Logic
{
    public class LogicData : Data
    {
        public int RemainingJump;

        public bool IsEndAbility = false;
        public bool IsDashing = false;
        public bool CanDash = true;
        
        public bool IsInflictEffect = false;
        public bool IsGetDamage = false;
        public bool IsRunning = false;
        public bool IsCrouching = false;
    }
}