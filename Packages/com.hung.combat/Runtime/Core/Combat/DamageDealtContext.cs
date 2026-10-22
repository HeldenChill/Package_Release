using Hung.Base;
using UnityEngine;

namespace Hung.Combat
{
    public struct DamageDealtContext
    {
        public object VictimHost;
        public GameObject VictimObject;
        public object Source;
        public DamageHit Hit;
        public FinalDamageInfo FinalDamage;
        public bool WillKill;
        public bool SuppressElementalProc;

        public bool HasDamageType(ELEMENTAL type)
        {
            if (Hit.Types == null)
            {
                return false;
            }

            for (int i = 0; i < Hit.Types.Length; i++)
            {
                if (Hit.Types[i] == type)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPetSourcedDamage => Source is IGameplayDamageSource;
    }
}
