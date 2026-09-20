using UnityEngine;
using Gameplay.Character.BrainSystem;

namespace Gameplay.Character.Logic
{
    using System;
    using Gameplay.Character.Physic;
    using Gameplay.Character.WorldInterface;
    using Gameplay.Character;
    using System.Collections.Generic;
    using Hung.DesignPattern;

    public class LogicParameter : Parameter
    {
        public bool IsUpdateRotation = true;
        public EFFECT Effect = EFFECT.NONE;
        public List<STATE> DecoratorStates = new List<STATE>();
        public Action<Type, string> OnAnimationTriggerEvent;
        public Action<Type, AnimationClip> OnReceiveAnimationClipData;
        public BrainData NavData;
        public WorldInterfaceData WIData;
        public PhysicData PhysicData;
    }
}