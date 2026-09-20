using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character
{
    [DefaultExecutionOrder(15)]
    public abstract class Module<D, P> : MonoBehaviour
        where D : Data
        where P : Parameter
    {
        protected D Data;
        protected P Parameter;
        public abstract void Initialize(D Data, P Parameter);
        public abstract void UpdateData();

        public virtual void FixedUpdateData()
        {

        }
    }
}