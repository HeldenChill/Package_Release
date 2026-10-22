using Hung.StatusEffect;
using UnityEngine;

namespace Hung.Combat
{
    public interface IStatusEffectHost : IModStat
    {
        public Transform Tf { get; }
        IStatusEffectVisualReceiver VisualReceiver { get; }
    }
}
