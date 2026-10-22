using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    public interface ISingleInstanceStackable
    {
        int StackCount { get; }
        int MaxStacks { get; }
        void PushStacks(int count);   // called when a duplicate is applied
        int PopStacks(int count);   // returns actual stacks removed; removes self when 0
    }
}
