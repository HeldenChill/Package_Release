using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public abstract class BrainModule : Module<BrainData, BrainParameter>
    {
        public override void Initialize(BrainData Data, BrainParameter Parameter)
        {
            this.Data = Data;
            this.Parameter = Parameter;
        }

        public abstract void StartNavigation();
        public abstract void StopNavigation();
        public virtual void ResetModule()
        {
            //Reset Data;
        }
    }
}
