using System;
using SStats;
using Hung.Data;

namespace Gameplay.Character
{
    [Serializable]
    public abstract class CharacterStats : ICharacterStats
    {
        protected const float VALUE_STEP = 0.02f;

        public Stat Speed;
        public Stat JumpSpeed;
        public Stat MaxHp;
        public Stat Sensitivity;
        public Stat MaxStamina;
        public Stat StaminaRegen;
        public Stat StaminaRegenStartTime;
        public Stat StaminaConsume;
        public Stat TiredStateTime;
        [NonSerialized] public TrackingStat Hp;
        [NonSerialized] public TrackingStat Stamina;

        public void InitBase(CharacterDefinition def)
        {
            Speed = new Stat(def.speed);
            JumpSpeed = new Stat(def.jumpSpeed);
            MaxHp = new Stat(def.maxHp);
            Sensitivity = new Stat(def.sensitivity);
            MaxStamina = new Stat(def.maxStamina);
            StaminaRegen = new Stat(def.staminaRegen);
            StaminaRegenStartTime = new Stat(def.staminaRegenStartTime);
            StaminaConsume = new Stat(def.staminaConsume);
            TiredStateTime = new Stat(def.tiredStateTime);
            Hp = new TrackingStat(MaxHp, new Stat(0));
            Stamina = new TrackingStat(MaxStamina, new Stat(0));
        }

        public virtual void Reset()
        {
            Speed.Reset();
            JumpSpeed.Reset();
            MaxHp.Reset();
            Sensitivity.Reset();
            MaxStamina.Reset();
            StaminaRegen.Reset();
            StaminaRegenStartTime.Reset();
            StaminaConsume.Reset();
            TiredStateTime.Reset();
            Hp.Reset();
            Stamina.Reset();
        }
    }
}
