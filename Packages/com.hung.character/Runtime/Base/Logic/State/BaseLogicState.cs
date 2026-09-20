namespace Gameplay.Character
{
    using UnityEngine;
    using Gameplay.Character.Logic;
    using Hung.DesignPattern;

    public abstract class BaseLogicState<D, P, E> : BaseState
        where D : LogicData
        where P : LogicParameter
        where E : LogicEvent
    {
        protected P Parameter;
        protected D Data;
        protected E Event;
        private CharacterStats stats;
        protected BaseState decorator;
        public override BaseState Decorator
        {
            get => decorator;
            set => decorator = value;
        }
        public override STATE_TYPE Type => STATE_TYPE.NORMAL;
        public T Stats<T>() where T : CharacterStats => (T)(stats ??= Parameter.GetStats<T>());

        public BaseLogicState(D data, P parameter, E _event)
        {
            Parameter = parameter;
            Data = data;
            Event = _event;
        }
        public override bool Update()
        {
            decorator?.Update();
            if (Stats<CharacterStats>().Hp.Value <= 0)
            {
                ChangeState(STATE.DIE);
                return false;
            }
            // if (Parameter.NavData.UsingSkill.Value)
            // {
            //     ChangeState(STATE.USING_SKILL);
            //     return true;
            // }
            return true;
        }
        public override bool FixedUpdate()
        {
            decorator?.FixedUpdate();
            return true;
        }
        
    }
}
