namespace Hung.StatusEffect
{
    public interface ISynergyUnlockProvider
    {
        bool IsUnlocked(string unlockKey, SynergyUnlockScope scope, SynergyUnlockContext context);
    }
}
