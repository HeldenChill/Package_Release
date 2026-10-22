using Hung.Base;
using UnityEngine;

[System.Serializable]
public class ShockStatusEffect : StatusEffect, ISingleInstanceStackable
{
    public int StackCount => baseDamage < 1f ? 1 : (int)baseDamage;
    public int MaxStacks => maxStacks;
    public void PushStacks(int count)
    {
        baseDamage = Mathf.Min(baseDamage + count, maxStacks);
    }
    public int PopStacks(int count)
    {
        int actual = (int)Mathf.Min(count, baseDamage);
        baseDamage -= actual;
        return actual;
    }
    public override StatusEffect Clone()
    {
        var clone = new ShockStatusEffect();
        clone.CopyBaseFields(this);
        return clone;
    }
}
