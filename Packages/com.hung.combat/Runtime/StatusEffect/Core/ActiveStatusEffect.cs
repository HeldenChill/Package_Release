using Hung.Combat;
using UnityEngine;

[System.Serializable]
public class ActiveStatusEffect
{
    public string effectId;
    public StatusEffect statusEffect;
    public IStatusEffectHost target;
    public GameObject caster;
    public float startTime;
    public float lastTickTime;

    public ActiveStatusEffect()
    {
        effectId = System.Guid.NewGuid().ToString();
    }
}
