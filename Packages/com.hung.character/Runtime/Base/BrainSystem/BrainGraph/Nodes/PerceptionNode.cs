using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class PerceptionNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.Perception;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            if (!context.HasWorldInterfaceData())
            {
                context.SetMessage("Perception failed: WorldInterfaceData is null.");
                return BrainGraphStatus.Failure;
            }

            var wi = context.GetWorldInterfaceData();
            context.Parameter.SoundPositions.Clear();
            context.Parameter.SoundWeights.Clear();
            context.Parameter.SeenPositions.Clear();
            context.Parameter.SeenWeights.Clear();

            if (wi.SoundColliders != null)
            {
                for (int i = 0; i < wi.SoundColliders.Count; i++)
                {
                    Collider c = wi.SoundColliders[i];
                    if (c == null) continue;
                    context.Parameter.SoundPositions.Add(c.transform.position);
                    context.Parameter.SoundWeights.Add(1f);
                }
            }

            if (wi.SeenColliders != null)
            {
                for (int i = 0; i < wi.SeenColliders.Count; i++)
                {
                    Collider c = wi.SeenColliders[i];
                    if (c == null) continue;
                    context.Parameter.SeenPositions.Add(c.transform.position);
                    context.Parameter.SeenWeights.Add(1f);
                }
            }

#if UNITY_EDITOR
            if (context.DebugRuntime)
            {
                int seen = context.Parameter.SeenPositions.Count;
                int sound = context.Parameter.SoundPositions.Count;
                context.SetMessage(seen == 0 && sound == 0 ? "Perception: idle" : $"Perception: seen={seen}, sound={sound}");
            }
#endif
            return BrainGraphStatus.Success;
        }
    }
}
