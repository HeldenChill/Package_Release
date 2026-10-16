using System;
using System.Collections.Generic;
using Hung.AI;
using UnityEngine;
using BrainData = Gameplay.Character.BrainSystem.BrainData;
namespace Gameplay.Character.AIBridge
{
    /// <summary>BrainData fields the bridge can copy to or from the blackboard.</summary>
    public enum BrainDataField
    {
        MoveTarget = 0,
        SeenEnemyPosition = 1,
        IsSeePosition = 2,
        IsSeenEnemyCollider = 3,
        IsRun = 4,
        IsCrouch = 5,
        Dash = 6,
        Attack1 = 7,
        Attack2 = 8,
        Attack3 = 9,
        PhaseId = 10,
        MoveSpeed = 11
    }

    public enum BrainDataDirection
    {
        BlackboardToBrainData = 0,
        BrainDataToBlackboard = 1
    }

    [Serializable]
    public sealed class BrainDataBinding
    {
        public BlackboardKeyRef Key = new BlackboardKeyRef();
        public BrainDataField Field;
        public BrainDataDirection Direction;
    }

    /// <summary>
    /// Copies chosen BrainData fields into the blackboard before each tick (BrainDataToBlackboard) and chosen
    /// blackboard keys into BrainData after each tick (BlackboardToBrainData), so game code that reads BrainData keeps working.
    /// </summary>
    [Serializable]
    public sealed class BrainDataMap
    {
        [SerializeField] List<BrainDataBinding> bindings = new List<BrainDataBinding>();
        [NonSerialized] HashSet<string> warned;

        public List<BrainDataBinding> Bindings => bindings ??= new List<BrainDataBinding>();

        public void Pull(BrainData data, Blackboard blackboard)
        {
            if (data == null || blackboard == null || bindings == null) return;
            for (int i = 0; i < bindings.Count; i++)
            {
                BrainDataBinding binding = bindings[i];
                if (binding == null || binding.Direction != BrainDataDirection.BrainDataToBlackboard) continue;
                int index = Resolve(blackboard, binding);
                if (index < 0) continue;
                switch (binding.Field)
                {
                    case BrainDataField.MoveTarget: blackboard.Set(index, data.MoveTarget); break;
                    case BrainDataField.SeenEnemyPosition: blackboard.Set(index, data.SeenEnemyPosition); break;
                    case BrainDataField.IsSeePosition: blackboard.Set(index, data.IsSeePosition); break;
                    case BrainDataField.IsSeenEnemyCollider: blackboard.Set(index, data.IsSeenEnemyCollider); break;
                    case BrainDataField.IsRun: blackboard.Set(index, data.IsRun); break;
                    case BrainDataField.IsCrouch: blackboard.Set(index, data.IsCrouch); break;
                    case BrainDataField.Dash: blackboard.Set(index, data.Dash); break;
                    case BrainDataField.Attack1: blackboard.Set(index, data.Attack1); break;
                    case BrainDataField.Attack2: blackboard.Set(index, data.Attack2); break;
                    case BrainDataField.Attack3: blackboard.Set(index, data.Attack3); break;
                    case BrainDataField.PhaseId: blackboard.Set(index, data.PhaseId); break;
                    case BrainDataField.MoveSpeed: blackboard.Set(index, data.MoveSpeed); break;
                }
            }
        }

        public void Push(Blackboard blackboard, BrainData data)
        {
            if (data == null || blackboard == null || bindings == null) return;
            for (int i = 0; i < bindings.Count; i++)
            {
                BrainDataBinding binding = bindings[i];
                if (binding == null || binding.Direction != BrainDataDirection.BlackboardToBrainData) continue;
                int index = Resolve(blackboard, binding);
                if (index < 0) continue;
                switch (binding.Field)
                {
                    case BrainDataField.MoveTarget: data.MoveTarget = blackboard.Get<Vector3>(index); break;
                    case BrainDataField.SeenEnemyPosition: data.SeenEnemyPosition = blackboard.Get<Vector3>(index); break;
                    case BrainDataField.IsSeePosition: data.IsSeePosition = blackboard.Get<bool>(index); break;
                    case BrainDataField.IsSeenEnemyCollider: data.IsSeenEnemyCollider = blackboard.Get<bool>(index); break;
                    case BrainDataField.IsRun: data.IsRun = blackboard.Get<bool>(index); break;
                    case BrainDataField.IsCrouch: data.IsCrouch = blackboard.Get<bool>(index); break;
                    case BrainDataField.Dash: data.Dash = blackboard.Get<bool>(index); break;
                    case BrainDataField.Attack1: data.Attack1 = blackboard.Get<bool>(index); break;
                    case BrainDataField.Attack2: data.Attack2 = blackboard.Get<bool>(index); break;
                    case BrainDataField.Attack3: data.Attack3 = blackboard.Get<bool>(index); break;
                    case BrainDataField.PhaseId: data.PhaseId = blackboard.Get<int>(index); break;
                    case BrainDataField.MoveSpeed: data.MoveSpeed = blackboard.Get<float>(index); break;
                }
            }
        }

        int Resolve(Blackboard blackboard, BrainDataBinding binding)
        {
            if (binding.Key == null || !binding.Key.IsAssigned) return -1;
            int index = blackboard.IndexOf(binding.Key);
            if (index < 0) return -1;
            BlackboardValueType actual = blackboard.GetValueType(index);
            if (Matches(binding.Field, actual)) return index;
            string token = binding.Key.KeyId + ":" + binding.Field;
            if ((warned ??= new HashSet<string>()).Add(token))
                Debug.LogWarning($"[Hung.AI] BrainData binding for {binding.Field} needs a {Expected(binding.Field)} key; '{blackboard.GetKeyName(index)}' is {actual}. The binding is skipped.");
            return -1;
        }

        static BlackboardValueType Expected(BrainDataField field)
        {
            switch (field)
            {
                case BrainDataField.MoveTarget:
                case BrainDataField.SeenEnemyPosition: return BlackboardValueType.Vector3;
                case BrainDataField.PhaseId: return BlackboardValueType.Int;
                case BrainDataField.MoveSpeed: return BlackboardValueType.Float;
                default: return BlackboardValueType.Bool;
            }
        }

        static bool Matches(BrainDataField field, BlackboardValueType actual)
        {
            BlackboardValueType expected = Expected(field);
            return actual == expected || (expected == BlackboardValueType.Int && actual == BlackboardValueType.Enum);
        }
    }
}
