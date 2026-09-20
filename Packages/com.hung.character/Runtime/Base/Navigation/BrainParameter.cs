
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gameplay.Character.BrainSystem
{
    using Gameplay.Character.WorldInterface;
    using UnityEngine.AI;

    public class BrainParameter : Parameter
    {
        public WorldInterfaceData WIData;
        public readonly List<Vector3> SoundPositions = new();
        public readonly List<float> SoundWeights = new();
        public readonly List<Vector3> SeenPositions = new();
        public readonly List<float> SeenWeights = new();
        public List<Vector3> MovingTargetPath = new();
    }
}
