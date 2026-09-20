using UnityEngine;
using Hung.Data;

namespace Gameplay.Character
{
    public abstract class CharacterDefinition : ICharacterDefinition
    {
        public float speed;
        public float jumpSpeed;
        public float maxHp;
        public float sensitivity;
        public float maxStamina;
        public float staminaRegen;
        public float staminaRegenStartTime;
        public float staminaConsume;
        public float tiredStateTime;

        public override abstract ICharacterStats BuildRuntime(ICharacterDefinition.BuildContext context = null);
    }
}
