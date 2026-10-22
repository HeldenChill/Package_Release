using UnityEngine;

[System.Serializable]
public class KnockBackStatusEffect : StatusEffect
{
    public float force;

    public override StatusEffect Clone()
    {
        var clone = new KnockBackStatusEffect();
        clone.CopyBaseFields(this);
        clone.force = force;
        clone.canStack = false;
        return clone;
    }
}
