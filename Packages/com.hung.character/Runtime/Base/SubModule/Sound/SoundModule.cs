using Hung.DesignPattern;
using UnityEngine;
using Hung.Utilities.Timer;

namespace Gameplay.Character
{
    public class SoundModule : MonoBehaviour
    {
        public void PlaySound(Vector3 position, float radius, float time = 0.4f)
        {
            SoundUnit unit = SimplePool.Spawn<SoundUnit>(PoolType.SOUND_UNIT, position, Quaternion.identity);
            unit.Init(radius);
            TimerManager.Ins.WaitForTime(time, () => DespawnSound(unit));
            
        }
        protected void DespawnSound(SoundUnit unit)
        {
            SimplePool.Despawn(unit);
        }
    }
}