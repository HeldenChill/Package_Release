using System;
using System.Linq;
using UnityEngine;

public class CharacterVisualModule : MonoBehaviour
{
    [SerializeField]
    protected Animator animator;

    public void SetAnimFloat(Type type, string name, float value)
    {
        if (HasAnimatorParameter(animator, name))
            animator.SetFloat(name, value);
    }

    bool HasAnimatorParameter(Animator animator, string paramName)
    {
        if (animator == null)
            return false;

        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }
}
