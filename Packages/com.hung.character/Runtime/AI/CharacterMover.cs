using Hung.AI;
using UnityEngine;
using UnityEngine.AI;
using BrainData = Gameplay.Character.BrainSystem.BrainData;
namespace Gameplay.Character.AIBridge
{
    /// <summary>
    /// IAIMover for Hung.Character bodies. It never moves the body: it writes BrainData.MoveDirection
    /// (world XZ, length MinSpeedRate..1 as a speed rate) and Logic/Physic move the character as they do for the player.
    /// With a NavMeshAgent it follows the agent's path (the agent must not move or turn the body); without one it heads straight at the target.
    /// </summary>
    public sealed class CharacterMover : IAIMover, IAIMovementCompletion
    {
        const float RepathDistanceSqr = 0.01f;
        readonly BrainData data;
        readonly Transform body;
        readonly NavMeshAgent navMeshAgent;
        float deltaTime;
        bool arrived;
        bool destinationRequested;
        Vector3 requestedDestination;
        float remaining = float.PositiveInfinity;

        public CharacterMover(BrainData data, Transform body, NavMeshAgent navMeshAgent = null)
        {
            this.data = data;
            this.body = body;
            this.navMeshAgent = navMeshAgent;
        }

        /// <summary>Flat distance at which the target counts as reached.</summary>
        public float ArriveDistance { get; set; } = 0.2f;

        /// <summary>Degrees per second the body turns toward its move direction. 0 disables turning.</summary>
        public float TurnSpeed { get; set; } = 360f;

        /// <summary>Speed rate written while the body still faces 45 degrees or more away from its move direction.</summary>
        public float MinSpeedRate { get; set; } = 0.25f;

        public bool HasArrived => arrived;

        public float RemainingDistance => remaining;

        /// <summary>Called by AIBrainModule once per brain tick, before the graph runs.</summary>
        public void BeginTick(float dt)
        {
            deltaTime = dt;
        }

        public bool SetDestination(Vector3 destination)
        {
            data.MoveTarget = destination;
            data.HasMoveTarget = true;
            Vector3 desired;
            if (navMeshAgent != null)
            {
                if (!navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
                {
                    Stop();
                    return false;
                }
                navMeshAgent.nextPosition = body.position;
                bool moved = (requestedDestination - destination).sqrMagnitude > RepathDistanceSqr;
                if (moved || !destinationRequested)
                {
                    if (!navMeshAgent.SetDestination(destination))
                    {
                        Stop();
                        return false;
                    }
                    requestedDestination = destination;
                    destinationRequested = true;
                }
                if (navMeshAgent.pathPending)
                {
                    Halt();
                    return true;
                }
                if (navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    Stop();
                    return false;
                }
                remaining = navMeshAgent.remainingDistance;
                desired = navMeshAgent.desiredVelocity;
            }
            else
            {
                desired = destination - body.position;
                desired.y = 0f;
                remaining = desired.magnitude;
            }
            arrived = remaining <= ArriveDistance;
            desired.y = 0f;
            data.MoveDirection = arrived || desired.sqrMagnitude < 0.0001f ? Vector3.zero : Steer(desired.normalized);
            WriteArrival();
            return true;
        }

        public void Stop()
        {
            Halt();
            data.HasMoveTarget = false;
            destinationRequested = false;
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
        }

        public void CompleteMovement()
        {
            float completedDistance = remaining;
            Stop();
            arrived = true;
            remaining = completedDistance;
            WriteArrival();
        }

        Vector3 Steer(Vector3 direction)
        {
            if (TurnSpeed > 0f && deltaTime > 0f)
                body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), TurnSpeed * deltaTime);
            Vector3 facing = body.forward;
            facing.y = 0f;
            float angle = Vector3.Angle(facing, direction);
            float rate = Mathf.Lerp(MinSpeedRate, 1f, Mathf.InverseLerp(45f, 0f, angle));
            return direction * rate;
        }

        void Halt()
        {
            arrived = false;
            remaining = float.PositiveInfinity;
            data.MoveDirection = Vector3.zero;
            WriteArrival();
        }

        void WriteArrival()
        {
            data.HasArrivedAtDestination = arrived;
            data.RemainingDistance = remaining;
        }
    }
}
