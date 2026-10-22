using UnityEngine;

namespace Hung.Combat
{
    public static class CombatHostExtensions
    {
        public static bool TryGetCombatDamageable(this IStatusEffectHost host, out ICombatDamageable damageable)
        {
            damageable = null;
            if (host == null)
            {
                return false;
            }

            if (host is ICombatDamageable direct)
            {
                damageable = direct;
                return true;
            }

            if (host is Component component && component.TryGetComponent(out ICombatDamageable found))
            {
                damageable = found;
                return true;
            }

            return false;
        }

        public static bool TryGetDamageReceiver(this IStatusEffectHost host, out IDamageReceiver receiver)
        {
            if (!TryGetCombatDamageable(host, out ICombatDamageable damageable))
            {
                receiver = null;
                return false;
            }

            receiver = damageable as IDamageReceiver;
            return receiver != null;
        }

        public static GameObject GetGameObject(this IStatusEffectHost host) =>
            host is Component component ? component.gameObject : null;

        public static bool TryGetMaxHealth(this IStatusEffectHost host, out float maxHealth)
        {
            maxHealth = 0f;
            if (host is IMaxHealthProvider provider)
            {
                maxHealth = provider.MaxHealth;
                return maxHealth > 0f;
            }

            return false;
        }

        public static bool TryGetDamageTakenMultiplierReceiver(
            this IStatusEffectHost host,
            out IDamageTakenMultiplierReceiver receiver)
        {
            receiver = host as IDamageTakenMultiplierReceiver;
            if (receiver != null)
            {
                return true;
            }

            if (host is Component component && component.TryGetComponent(out IDamageTakenMultiplierReceiver found))
            {
                receiver = found;
                return true;
            }

            receiver = null;
            return false;
        }
    }
}
