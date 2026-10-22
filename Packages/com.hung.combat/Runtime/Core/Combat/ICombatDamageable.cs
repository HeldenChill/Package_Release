namespace Hung.Combat
{
    public interface ICombatDamageable
    {
        float TakeDamage(in DamageHit hit);
    }
}
