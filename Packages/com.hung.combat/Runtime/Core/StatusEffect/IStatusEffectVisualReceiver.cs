namespace Hung.StatusEffect
{
    public interface IStatusEffectVisualReceiver
    {
        void OnStatusStackApplied(StatusEffectTag tag);
        void OnStatusStackRemoved(StatusEffectTag tag);
        void OnSynergyStatusTrigger(SynergyEffectTag data);
    }
}
