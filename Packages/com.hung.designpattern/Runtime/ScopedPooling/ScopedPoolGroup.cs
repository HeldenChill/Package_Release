using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern.ScopedPooling
{
    internal sealed class ScopedPoolGroup
    {
        internal readonly Transform Container, Staging;
        internal readonly List<Bucket> Buckets = new List<Bucket>();

        internal ScopedPoolGroup(Transform root, string name)
        {
            Container = new GameObject("ScopedPool-" + name).transform;
            Container.SetParent(root, false);
            Staging = new GameObject("InactiveStaging").transform;
            Staging.gameObject.SetActive(false);
            Staging.SetParent(Container, false);
        }

        internal sealed class Bucket
        {
            internal PoolDefinition Definition;
            internal ScopedPoolGroup Group;
            internal readonly List<Slot> All = new List<Slot>();
            internal readonly Queue<Slot> Inactive = new Queue<Slot>();
        }

        internal sealed class Slot
        {
            internal long Id;
            internal ulong Generation;
            internal Component Instance;
            internal bool Active;
            internal Bucket Bucket;
        }

        internal void Destroy()
        {
            foreach (var bucket in Buckets)
                foreach (var slot in bucket.All)
                    if (slot.Instance != null) DestroyOwned(slot.Instance.gameObject);
            if (Container != null) DestroyOwned(Container.gameObject);
        }

        internal static void DestroyOwned(GameObject value)
        {
            value.SetActive(false);
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
