using UnityEngine;

[System.Serializable]
public class ElectricStatusEffect : StatusEffect
{
    public override StatusEffect Clone()
    {
        var clone = new ElectricStatusEffect();
        clone.CopyBaseFields(this);
        return clone;
    }
}
