using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hung.Base
{
    public class Target3DDetection<T> : BaseTargetDetection<T>
    {
        protected string[] DETECTED_LAYERS;
        //[SerializeField]
        protected Collider detectCollider;
        protected Collider[] targetsDetected;

        public T IgnoreDamageable;
        public int Condition = 0; //NOTE: The number of collider ignore(collider that alway collide with this detect collider)
        public int targetMask;

        public Target3DDetection(string[] layerNames, T IgnoreDamageable = default, int maxDetectedCollider = 10, Collider detectCollider = null)
        {
            DETECTED_LAYERS = layerNames;
            this.detectCollider = detectCollider;
            this.IgnoreDamageable = IgnoreDamageable;

            targetsDetected = new Collider[maxDetectedCollider];

            targetMask = LayerMask.GetMask(DETECTED_LAYERS);
        }

        public override void CheckTargets(Vector3 position, float radius, ref List<T> targets)
        {
            targets.Clear();
            Array.Clear(targetsDetected, 0, targetsDetected.Length);
            //int t = Physics2D.OverlapCollider(detectCollider, contactFilter2D, targetsDetected);
            int t = UnityEngine.Physics.OverlapSphereNonAlloc(position, radius, targetsDetected, targetMask);
            //NOTE: Detect collide position 
            if (t > Condition)
            {
                for (int i = 0; i < targetsDetected.Length; i++)
                {
                    if (targetsDetected[i] == null) continue;

                    T target = targetsDetected[i].GetComponent<T>(); //NOTE: Need To Cache
                    if (target != null)
                    {
                        if (target.GetHashCode() != IgnoreDamageable?.GetHashCode())//NOTE: Ignore self takedamage
                        {
                            targets.Add(target);
                        }
                    }
                }
            }
        }

        public override void CheckTargets(Vector3 position, Vector3 size, Quaternion angle, ref List<T> targets)
        {
            targets.Clear();
            Array.Clear(targetsDetected, 0, targetsDetected.Length);
            //int t = Physics2D.OverlapCollider(detectCollider, contactFilter2D, targetsDetected);
            int t = UnityEngine.Physics.OverlapBoxNonAlloc(position, size, targetsDetected, angle, targetMask);
            //NOTE: Detect collide position 
            if (t > Condition)
            {
                for (int i = 0; i < targetsDetected.Length; i++)
                {
                    if (targetsDetected[i] == null) continue;

                    T target = targetsDetected[i].GetComponent<T>(); //NOTE: Need To Cache
                    if (target != null)
                    {
                        if (target.GetHashCode() != IgnoreDamageable?.GetHashCode())//NOTE: Ignore self takedamage
                        {
                            targets.Add(target);
                        }
                    }
                }
            }
        }
        public override void CheckTargets(ref List<T> targets)
        {
            if (detectCollider == null)
            {
                Debug.LogError("Target Detection Collider NULL");
                return;
            }

            targets.Clear();
            Array.Clear(targetsDetected, 0, targetsDetected.Length);
            detectCollider.enabled = true;
            int t = 0;

            switch (detectCollider.GetType())
            {
                case Type type when type == typeof(BoxCollider):
                    t = UnityEngine.Physics.OverlapBoxNonAlloc(detectCollider.transform.position + detectCollider.bounds.center, detectCollider.bounds.size / 2, targetsDetected, detectCollider.transform.rotation, targetMask);
                    break;
                case Type type when type == typeof(SphereCollider):
                    t = UnityEngine.Physics.OverlapSphereNonAlloc(detectCollider.transform.position + detectCollider.bounds.center, ((SphereCollider)detectCollider).radius, targetsDetected, targetMask);
                    break;
            }
            //NOTE: Detect collide position 
            if (t > Condition)
            {
                for (int i = 0; i < targetsDetected.Length; i++)
                {
                    if (targetsDetected[i] == null) continue;

                    T target = targetsDetected[i].GetComponent<T>(); //NOTE: Need To Cache
                    if (target != null)
                    {
                        if (target.GetHashCode() != IgnoreDamageable?.GetHashCode())//NOTE: Ignore self takedamage
                        {
                            targets.Add(target);
                        }
                    }
                }
            }
            detectCollider.enabled = false;
        }

        public virtual void CheckTargets(ref List<T> targets, Collider detectCollider)
        {
            if (detectCollider == null)
            {
                Debug.LogError("Target Detection Collider NULL");
                return;
            }

            targets.Clear();
            Array.Clear(targetsDetected, 0, targetsDetected.Length);
            detectCollider.enabled = true;
            int t = 0;

            switch (detectCollider.GetType())
            {
                case Type type when type == typeof(BoxCollider):
                    BoxCollider boxCollider = detectCollider as BoxCollider;
                    t = Physics.OverlapBoxNonAlloc(boxCollider.transform.TransformPoint(boxCollider.center), boxCollider.size / 2, targetsDetected, detectCollider.transform.rotation, targetMask);
                    break;
                case Type type when type == typeof(SphereCollider):
                    SphereCollider sphereCollider = detectCollider as SphereCollider;
                    t = Physics.OverlapSphereNonAlloc(sphereCollider.transform.TransformPoint(sphereCollider.center), sphereCollider.radius, targetsDetected, targetMask);
                    break;
            }
            //NOTE: Detect collide position 
            if (t > Condition)
            {
                for (int i = 0; i < targetsDetected.Length; i++)
                {
                    if (targetsDetected[i] == null) continue;

                    T target = targetsDetected[i].GetComponent<T>(); //NOTE: Need To Cache
                    if (target != null)
                    {
                        if (target.GetHashCode() != IgnoreDamageable?.GetHashCode())//NOTE: Ignore self takedamage
                        {
                            targets.Add(target);
                        }
                    }
                }
            }
            detectCollider.enabled = false;
        }
    }
}
