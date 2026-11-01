using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Hung.Cinematics
{
    public sealed class CinematicBindings
    {
        public CinemachineBrain Brain;
        public CinemachineCamera CaptureCamera;
        public Pose Frame;
        public IReadOnlyDictionary<string, Transform> Transforms;
        public IReadOnlyDictionary<string, AudioSource> Audio;
    }
}
