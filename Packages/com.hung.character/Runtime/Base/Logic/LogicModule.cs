using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.Logic
{
    public abstract class LogicModule : Module<LogicData, LogicParameter>
    {
        protected LogicEvent Event;
        
        public override void Initialize(LogicData Data, LogicParameter Parameter)
        {
            this.Parameter = Parameter;
            this.Data = Data;
        }

        public virtual void Initialize(LogicData Data, LogicParameter Parameter, LogicEvent Event)
        {
            this.Event = Event;
            Initialize(Data, Parameter);
        }
    }
}