using Hung.Base;
using SStats;

public interface IModStat
{
    bool IsStatusTargetAlive { get; }
    void AddStatModifier(STAT stat, StatModifier modifier, bool isShowUI = true);
    float GetCurrentStat(STAT stat);
    void RemoveStatModifier(STAT stat, StatModifier modifier, bool isShowUI = true);
}
