using Gameplay.Character;

namespace Gameplay.Character
{
    public class Parameter
    {
        protected CharacterStats stats;
        public PerceptionData PerceptionData;

        public void SetStats<T>(T value) where T : CharacterStats
        {
            stats = value;
        }
        public T GetStats<T>() where T : CharacterStats
        {
            return (T)stats;
        }
    }
}