using UnityEngine;
namespace Hung.AI.Tests
{
    public sealed class FakeMoverBehaviour : MonoBehaviour, IAIMover
    {
        public int StopCalls;

        public bool SetDestination(Vector3 destination) => true;

        public void Stop() => StopCalls++;

        public bool HasArrived => false;

        public float RemainingDistance => 10f;
    }
}
