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
        private static readonly Dictionary<Object, Pool> pools = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            pools.Clear();
            root = null;
        }

        public static Transform Root
        {
            get
            {
                if (root == null)
                {
                    // Null-safe like SimplePool.Root: no PoolController in the scene is allowed (BUG-261114-2ef8).
                    PoolController controller = Object.FindObjectOfType<PoolController>();
                    root = controller != null ? controller.transform : new GameObject("ParticlePool").transform;
                }

                return root;
            }
        }

        /// <summary>
        ///     Init our dictionary.
        /// </summary>
        private static void Init(ParticleSystem prefab = null, int qty = DEFAULT_POOL_SIZE, Transform parent = null)
        {
            if (prefab != null && !pools.ContainsKey(prefab))
                pools[prefab] = new Pool(prefab, qty, parent);
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

            if (!pools.ContainsKey(prefab))
            {
                Transform newRoot = new GameObject("VFX_" + prefab.name).transform;
                newRoot.SetParent(Root);
                pools[prefab] = new Pool(prefab, 10, newRoot);
            }

            return pools[prefab].Play(pos, rot);
        }

        public static void Release(ParticleSystem prefab)
        {
            if (pools.ContainsKey(prefab))
            {
                pools[prefab].Release();
                pools.Remove(prefab);
            }
            // An unpooled prefab is left alone: it may be an asset (BUG-261114-0e6f).
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
                m_sRoot = parent;
                this.prefab = prefab;
                inactive = new List<ParticleSystem>(initialQty);

                for (int i = 0; i < initialQty; i++)
                {
                    inactive.Add(CreateCopy());
                }
            }

            public int Count => inactive.Count;

            // The pool reuses a copy once it stops playing, so copies (never the prefab asset) are made
            // non-looping and kept alive on stop, in every build (BUG-261114-0b41).
            private ParticleSystem CreateCopy()
            {
                ParticleSystem copy = Object.Instantiate(prefab, m_sRoot);
                foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>(true))
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ParticleSystem.MainModule main = system.main;
                    main.loop = false;
                    main.stopAction = ParticleSystemStopAction.None;
                }
                return copy;
            }

            // Spawn an object from our pool
            public ParticleSystem Play(Vector3 pos, Quaternion rot)
            {
                index = index + 1 < inactive.Count ? index + 1 : 0;

                ParticleSystem obj = inactive[index];

                if (obj.isPlaying)
                {
                    obj = CreateCopy();
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
                    if (inactive[0] != null) Object.DestroyImmediate(inactive[0].gameObject);
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
