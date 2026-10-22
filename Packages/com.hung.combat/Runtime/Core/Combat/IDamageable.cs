using Hung.Combat;

namespace Hung.Base
{
    /// <summary>Targeting + combat damage + logic/CC effects.</summary>
    public interface IDamageable : IDamageTarget, ICombatDamageable, ILogicEffectReceiver
    {
    }
}
