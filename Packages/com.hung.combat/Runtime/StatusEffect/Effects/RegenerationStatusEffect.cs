using UnityEngine;

[System.Serializable]
public class RegenerationStatusEffect : StatusEffect
{
    public float healAmount;

    public override StatusEffect Clone()
    {
        var clone = new RegenerationStatusEffect();
        clone.CopyBaseFields(this);
        clone.healAmount = healAmount;
        return clone;
    }
}
