using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    using System;
    public class Target2DDetection<T> : BaseTargetDetection<T>
    {
        protected string[] DETECTED_LAYERS;
        //[SerializeField]
        protected Collider2D detectCollider;
        protected Collider2D[] targetsDetected;
        protected ContactFilter2D contactFilter2D;

        public bool IsTrigger
        {
            get => contactFilter2D.useTriggers;
            set
            {
                contactFilter2D.useTriggers = value;
            }
        }
        public T IgnoreDamageable;
        public int Condition = 0; //NOTE: The number of collider ignore(collider that alway collide with this detect collider)

        public Target2DDetection(string[] layerNames,T IgnoreDamageable = default, int maxDetectedCollider = 10, Collider2D detectCollider = null)
        {
            DETECTED_LAYERS = layerNames;
            this.detectCollider = detectCollider;
            this.IgnoreDamageable = IgnoreDamageable;

            targetsDetected = new Collider2D[maxDetectedCollider];
            contactFilter2D = new ContactFilter2D();

            contactFilter2D.layerMask = LayerMask.GetMask(DETECTED_LAYERS);
            contactFilter2D.useLayerMask = true;
            contactFilter2D.useTriggers = true;
        }

        public override void CheckTargets(Vector3 position,float radius,ref List<T> targets)
        {
            targets.Clear();
            Array.Clear(targetsDetected, 0, targetsDetected.Length);
            //int t = Physics2D.OverlapCollider(detectCollider, contactFilter2D, targetsDetected);
            int t = Physics2D.OverlapCircleNonAlloc(position, radius, targetsDetected, contactFilter2D.layerMask);
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
            int t = Physics2D.OverlapBoxNonAlloc(position, size, angle.eulerAngles.z, targetsDetected, contactFilter2D.layerMask);
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
            if(detectCollider == null)
            {
                Debug.LogError("Target Detection Collider NULL");
                return;
            }

            targets.Clear();
            Array.Clear(targetsDetected, 0, targetsDetected.Length);
            detectCollider.enabled = true;
            int t = Physics2D.OverlapCollider(detectCollider, contactFilter2D, targetsDetected);
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

        public virtual void CheckTargets(ref List<T> targets, Collider2D detectCollider)
        {
            if(detectCollider == null)
            {
                Debug.LogError("Target Detection Collider NULL");
                return;
            }

            targets.Clear();
            Array.Clear(targetsDetected, 0, targetsDetected.Length);
            detectCollider.enabled = true;
            int t = Physics2D.OverlapCollider(detectCollider, contactFilter2D, targetsDetected);
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