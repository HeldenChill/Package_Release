using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Character.BrainSystem
{
    // Shared helpers used by more than one node handler. Single-node helpers stay private in their
    // own node file; anything two+ nodes need lives here.
    internal static class BrainGraphNodeHelpers
    {
        public const float MinSpeedRate = 0.25f;

        public static void UpdateSeenTarget(BrainGraphExecutionContext context)
        {
            if (context.Parameter.SeenPositions.Count == 0) return;

            int index = PickWeightedIndex(context.Parameter.SeenWeights);
            Vector3 target = context.Parameter.SeenPositions[index] + Vector3.up * 0.5f;
            context.Data.SeenEnemyPosition = target;
            context.Data.MoveTarget = target;
            context.Data.HasMoveTarget = true;

            var wi = context.GetWorldInterfaceData();
            if (wi != null && wi.SeenColliders != null && index >= 0 && index < wi.SeenColliders.Count)
                context.LastSeenCollider = wi.SeenColliders[index];
        }

        public static Vector3 PickWeightedPosition(IList<Vector3> positions, IList<float> weights, Vector3 fallback)
        {
            if (positions == null || positions.Count == 0) return fallback;
            int index = PickWeightedIndex(weights);
            if (index < 0 || index >= positions.Count) index = 0;
            return positions[index];
        }

        public static int PickWeightedIndex(IList<float> weights)
        {
            if (weights == null || weights.Count == 0) return 0;
            int index = 0;
            float min = float.MaxValue;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] < min)
                {
                    min = weights[i];
                    index = i;
                }
            }
            return index;
        }

        public static bool HasReachedDestination(BrainGraphExecutionContext context, NavMeshAgent agent, Vector3 desiredDir)
        {
            float dot = desiredDir == Vector3.zero || context.Transform == null ? 1f : Vector3.Dot(context.Transform.forward, desiredDir.normalized);
            if (agent.pathPending) return false;
            if (agent.remainingDistance > agent.stoppingDistance) return false;
            return !agent.hasPath || agent.velocity.sqrMagnitude == 0f || dot < 0f;
        }

        public static Vector3 GetVelocity(BrainGraphExecutionContext context, NavMeshAgent agent, Vector3 desiredDir, int nodeId)
        {
            if (desiredDir.sqrMagnitude <= 0.0001f) return Vector3.zero;

            Transform tf = context.Transform;
            Vector3 normalized = desiredDir.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(normalized);
            tf.rotation = Quaternion.RotateTowards(tf.rotation, targetRotation, agent.angularSpeed * Time.deltaTime);

            float angle = Vector3.Angle(tf.forward, normalized);
            float speedRate = Mathf.Clamp01(Mathf.Lerp(MinSpeedRate, 1f, Mathf.InverseLerp(45f, 0f, angle)));
            if (agent.remainingDistance < agent.stoppingDistance * 2f)
                speedRate = Mathf.Min(speedRate, 0.75f);
            return tf.forward * Mathf.Max(MinSpeedRate, speedRate);
        }
    }
}
