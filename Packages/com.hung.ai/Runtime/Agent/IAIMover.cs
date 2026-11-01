using UnityEngine;
namespace Hung.AI
{
    /// <summary>Movement abstraction used by movement nodes. Implement it with NavMesh, character physics, or anything else.</summary>
    public interface IAIMover
    {
        /// <summary>Requests movement toward destination. Returns false if the destination cannot be used (off mesh, unreachable).</summary>
        bool SetDestination(Vector3 destination);

        /// <summary>Stops movement and clears the current destination.</summary>
        void Stop();

        bool HasArrived { get; }

        /// <summary>Distance left along the current path; PositiveInfinity while unknown.</summary>
        float RemainingDistance { get; }
    }

    /// <summary>Optional completion channel for movers that expose an arrival result to their host.</summary>
    public interface IAIMovementCompletion
    {
        /// <summary>Stops movement and records successful arrival, including the movement node's distance tolerance.</summary>
        void CompleteMovement();
    }
}
