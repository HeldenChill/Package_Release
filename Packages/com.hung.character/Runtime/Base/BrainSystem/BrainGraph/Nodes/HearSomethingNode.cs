using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class HearSomethingNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.HearSomething;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            float memory = node.floatValue > 0f ? node.floatValue : context.Graph.soundMemorySeconds;
            ulong soundHash = ENVIRONMENT_HASH.Compute(context.Parameter.SoundPositions, context.Parameter.SoundWeights);
            bool hasNewSound = soundHash != 0 && soundHash != context.LastSoundHash;
            bool hasSound = context.Parameter.SoundPositions.Count > 0;
            bool hasMemory = Time.time <= context.SoundMemoryUntil;

            if (hasNewSound || hasSound)
            {
                context.LastSoundHash = soundHash;
                context.SoundMemoryUntil = Time.time + memory;
                context.LastSoundPosition = BrainGraphNodeHelpers.PickWeightedPosition(context.Parameter.SoundPositions, context.Parameter.SoundWeights, context.LastSoundPosition);
                context.Data.MoveTarget = context.LastSoundPosition;
                context.Data.HasMoveTarget = true;
                context.SetMessage("Hear sound.");
                return BrainGraphStatus.Success;
            }

            if (hasMemory)
            {
                context.Data.MoveTarget = context.LastSoundPosition;
                context.Data.HasMoveTarget = true;
                context.SetMessage("Hear sound from memory.");
                return BrainGraphStatus.Success;
            }

            context.SetMessage("Hear sound failed.");
            return BrainGraphStatus.Failure;
        }
    }
}
