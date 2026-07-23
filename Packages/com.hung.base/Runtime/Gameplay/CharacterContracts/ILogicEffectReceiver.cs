using UnityEngine;

namespace Hung.Base
{
    public interface ILogicEffectReceiver
    {
        float TakeDamage(EFFECT effect, float time, Vector3 sourcePos, float damage = 1, object source = null);
    }
}
