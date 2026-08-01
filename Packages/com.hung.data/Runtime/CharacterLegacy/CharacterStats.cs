using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities.Core.Data
{
    using SStats;

    public abstract class CharacterStats : ICharacterStats
    {
        public virtual string Id { get; set; }
        public virtual int Level { get; set; }
        public float Hp;
        public abstract void Reset();
    }
}