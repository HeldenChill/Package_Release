using UnityEngine;
using UnityEngine.AI;
namespace Hung.AI.NavMesh
{
    /// <summary>Default IAIMover backed by a NavMeshAgent. Add it next to AIAgent to opt in.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshMover : MonoBehaviour, IAIMover
    {
        const float RepathDistanceSqr = 0.01f;
        NavMeshAgent agent;

        NavMeshAgent Agent => agent != null ? agent : (agent = GetComponent<NavMeshAgent>());

        public bool SetDestination(Vector3 destination)
        {
            NavMeshAgent a = Agent;
            if (!a.isActiveAndEnabled || !a.isOnNavMesh) return false;
            a.isStopped = false;
            if (a.hasPath && (a.destination - destination).sqrMagnitude <= RepathDistanceSqr) return true;
            return a.SetDestination(destination);
        }

        public void Stop()
        {
            NavMeshAgent a = Agent;
            if (!a.isActiveAndEnabled || !a.isOnNavMesh) return;
            a.isStopped = true;
            a.ResetPath();
        }

        public bool HasArrived
        {
            get
            {
                NavMeshAgent a = Agent;
                return a.isOnNavMesh && !a.pathPending && a.remainingDistance <= a.stoppingDistance;
            }
        }

        public float RemainingDistance => Agent.pathPending ? float.PositiveInfinity : Agent.remainingDistance;
    }
}
