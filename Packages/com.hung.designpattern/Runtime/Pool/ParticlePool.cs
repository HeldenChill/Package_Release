using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hung.DesignPattern
{
    public static class ParticlePool
    {
        private const int DEFAULT_POOL_SIZE = 3;

        private static Transform root;

        //--------------------------------------------------------------------------------------------------

        // All of our pools
        private static readonly Dictionary<int, Pool> pools = new();

        public static Transform Root
        {
            get
            {
                if (root == null)
                {
                    root = Object.FindObjectOfType<PoolController>().transform;
                    if (root == null)
                    {
                        root = new GameObject().transform;
                        root.name = "ParticlePool";
                    }
                }

                return root;
            }
        }

        /// <summary>
        ///     Init our dictionary.
        /// </summary>
        private static void Init(ParticleSystem prefab = null, int qty = DEFAULT_POOL_SIZE, Transform parent = null)
        {
            if (prefab != null && !pools.ContainsKey(prefab.GetInstanceID()))
                pools[prefab.GetInstanceID()] = new Pool(prefab, qty, parent);
        }

        public static void Preload(ParticleSystem prefab, int qty = 1, Transform parent = null)
        {
            Init(prefab, qty, parent);
        }

        public static ParticleSystem Play(ParticleSystem prefab, Vector3 pos, Quaternion rot = default)
        {
            if (prefab == null)
            {
                Debug.Log("Prefab is null");
                return null;
            }

            if (!pools.ContainsKey(prefab.GetInstanceID()))
            {
                Transform newRoot = new GameObject("VFX_" + prefab.name).transform;
                newRoot.SetParent(Root);
                pools[prefab.GetInstanceID()] = new Pool(prefab, 10, newRoot);
            }

            return pools[prefab.GetInstanceID()].Play(pos, rot);
        }

        public static void Release(ParticleSystem prefab)
        {
            if (pools.ContainsKey(prefab.GetInstanceID()))
            {
                pools[prefab.GetInstanceID()].Release();
                pools.Remove(prefab.GetInstanceID());
            }
            else
            {
                Object.DestroyImmediate(prefab);
            }
        }

        /// <summary>
        ///     The Pool class represents the pool for a particular prefab.
        /// </summary>
        private class Pool
        {
            //list prefab ready
            private readonly List<ParticleSystem> inactive;
            private readonly Transform m_sRoot;

            // The prefab that we are pooling
            private readonly ParticleSystem prefab;

            private int index;

            // Constructor
            public Pool(ParticleSystem prefab, int initialQty, Transform parent)
            {
#if UNITY_EDITOR
                if (prefab.main.loop)
                {
                    ParticleSystem.MainModule main = prefab.main;
                    main.loop = false;

                    //save prefab
                    UnityEditor.Undo.RegisterCompleteObjectUndo(prefab, "Fix To Not Loop");
                    Debug.Log(prefab.name + " ~ Fix To Not Loop");
                }

                if (prefab.main.stopAction != ParticleSystemStopAction.None)
                {
                    ParticleSystem.MainModule main = prefab.main;
                    main.stopAction = ParticleSystemStopAction.None;

                    //save prefab
                    UnityEditor.Undo.RegisterCompleteObjectUndo(prefab, "Fix To Stop Action None");
                    Debug.Log(prefab.name + " ~ Fix To  Stop Action None");
                }

                if (prefab.main.duration > 2)
                {
                    ParticleSystem.MainModule main = prefab.main;
                    main.duration = 2;

                    //save prefab
                    UnityEditor.Undo.RegisterCompleteObjectUndo(prefab, "Fix To Duration By 1");
                    Debug.Log(prefab.name + " ~ Fix To Duration By 2");
                }
#endif

                m_sRoot = parent;
                this.prefab = prefab;
                inactive = new List<ParticleSystem>(initialQty);

                for (int i = 0; i < initialQty; i++)
                {
                    ParticleSystem particle = Object.Instantiate(prefab, m_sRoot);
                    particle.Stop();
                    inactive.Add(particle);
                }
            }

            public int Count => inactive.Count;

            // Spawn an object from our pool
            public ParticleSystem Play(Vector3 pos, Quaternion rot)
            {
                index = index + 1 < inactive.Count ? index + 1 : 0;

                ParticleSystem obj = inactive[index];

                if (obj.isPlaying)
                {
                    obj = Object.Instantiate(prefab, m_sRoot);
                    obj.Stop();
                    inactive.Insert(index, obj);
                }

                obj.transform.SetPositionAndRotation(pos, rot);
                obj.Play();
                return obj;
            }

            public void Release()
            {
                while (inactive.Count > 0)
                {
                    Object.DestroyImmediate(inactive[0]);
                    inactive.RemoveAt(0);
                }

                inactive.Clear();
            }
        }
    }

    [Serializable]
    public struct ParticleAmount
    {
        public Transform root;
        public ParticleSystem prefab;
        public int amount;
    }
}
