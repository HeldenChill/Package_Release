using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character
{
    using System;
    public abstract class System<M,D,P>
        where M : Module<D,P>
        where D : Data
        where P : Parameter
    {
        protected M module;
        protected D data;
        protected P Parameter;
        protected virtual void UpdateData()
        {
            module.UpdateData();
        }   
        public virtual D Data
        {
            get => data;
        }
        public virtual void FixedUpdateData()
        {
            module.FixedUpdateData();
        }
        public void Run()
        {
            UpdateData();
        }
    }
}