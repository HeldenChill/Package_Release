using System.Collections.Generic;
using UnityEngine;
using WorldInterfaceData = Gameplay.Character.WorldInterface.WorldInterfaceData;
namespace Gameplay.Character.AIBridge
{
    /// <summary>
    /// Per-character perception memory. Sense World fills it from the WorldInterface sensors every tick;
    /// the other perception nodes read it. Times are AI time (AIContext.Time), not Time.time.
    /// </summary>
    public sealed class CharacterPerception
    {
        public Collider SeenCollider;
        public Vector3 SeenPosition;
        public float LastSeenTime = float.NegativeInfinity;
        public bool SeesNow;
        public Vector3 SoundPosition;
        public float LastHeardTime = float.NegativeInfinity;
        public bool HearsNow;

        /// <summary>Keeps the nearest seen and the nearest heard collider. Lists may be null or hold destroyed colliders.</summary>
        public void Sense(WorldInterfaceData world, Vector3 origin, float time)
        {
            SeesNow = false;
            HearsNow = false;
            if (world == null) return;
            Collider seen = Nearest(world.SeenColliders, origin);
            if (seen != null)
            {
                SeenCollider = seen;
                SeenPosition = seen.transform.position;
                LastSeenTime = time;
                SeesNow = true;
            }
            Collider heard = Nearest(world.SoundColliders, origin);
            if (heard != null)
            {
                SoundPosition = heard.transform.position;
                LastHeardTime = time;
                HearsNow = true;
            }
        }

        public bool RemembersSeen(float time, float memorySeconds) => SeesNow || time - LastSeenTime <= memorySeconds;

        public bool RemembersSound(float time, float memorySeconds) => HearsNow || time - LastHeardTime <= memorySeconds;

        public void ForgetSeen()
        {
            SeenCollider = null;
            LastSeenTime = float.NegativeInfinity;
            SeesNow = false;
        }

        public void ForgetSound()
        {
            LastHeardTime = float.NegativeInfinity;
            HearsNow = false;
        }

        public void Clear()
        {
            ForgetSeen();
            ForgetSound();
        }

        static Collider Nearest(List<Collider> colliders, Vector3 origin)
        {
            if (colliders == null) return null;
            Collider best = null;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == null) continue;
                float sqr = (candidate.transform.position - origin).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = candidate;
            }
            return best;
        }
    }
}
