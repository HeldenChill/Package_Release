using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hung.DesignPattern
{
    public static class SimplePool
    {
        private const string PATH = "Pool";
        private const int DEFAULT_POOL_SIZE = 3;

        private static readonly Dictionary<int, Pool> Pools = new();

        private static readonly Dictionary<PoolType, GameUnit> PoolTypes = new();

        private static GameUnit[] _gameUnitResources;

        private static Transform _root;

        // All of our pools
        private static readonly Dictionary<int, Pool> PoolInstanceID = new();

        private static Transform Root
        {
            get
            {
                // Unity's overloaded == : the cached static survives a scene unload as "fake-null".
                if (_root != null) return _root;
                PoolController controller = Object.FindObjectOfType<PoolController>();
                _root = controller != null ? controller.transform : new GameObject(PATH).transform;
                return _root;
            }
        }

        private static void Init(GameUnit prefab = null, int qty = DEFAULT_POOL_SIZE, Transform parent = null,
            bool collect = false, bool clamp = false)
        {
            if (prefab != null && !IsHasPool(prefab.GetInstanceID()))
                PoolInstanceID.Add(prefab.GetInstanceID(), new Pool(prefab, qty, parent, collect, clamp));
        }

        private static bool IsHasPool(int instanceID)
        {
            return PoolInstanceID.ContainsKey(instanceID);
        }

        public static void Preload(GameUnit prefab, int qty = 1, Transform parent = null, bool collect = false,
            bool clamp = false)
        {
            if (prefab == null)
            {
                if (parent != null) Debug.LogError(parent.name + " : IS EMPTY!!!");
                return;
            }

            PoolTypes.TryAdd(prefab.PoolType, prefab);

            Init(prefab, qty, parent, collect, clamp);

            // Make an array to grab the objects we're about to pre-spawn.
            GameUnit[] obs = new GameUnit[qty];
            for (int i = 0; i < qty; i++) obs[i] = Spawn(prefab);

            // Now despawn them all.
            for (int i = 0; i < qty; i++) Despawn(obs[i]);
        }

        public static T Spawn<T>(PoolType poolType, Vector3 pos, Quaternion rot) where T : GameUnit
        {
            return Spawn(GetGameUnitByType(poolType), pos, rot) as T;
        }

        public static T Spawn<T>(PoolType poolType) where T : GameUnit
        {
            return Spawn<T>(GetGameUnitByType(poolType));
        }

        public static T Spawn<T>(GameUnit obj, Vector3 pos, Quaternion rot) where T : GameUnit
        {
            return Spawn(obj, pos, rot) as T;
        }

        public static T Spawn<T>(GameUnit obj) where T : GameUnit
        {
            return Spawn(obj) as T;
        }

        // Note: Only use this function when you are sure that this unit is already in pool
        public static GameUnit SpawnDirectFromPool(GameUnit obj, Vector3 pos, Quaternion rot)
        {
            if (!Pools.ContainsKey(obj.GetInstanceID()))
            {
                // NOTE: Comment it because we know that this unit is already in pool
                // Transform newRoot = new GameObject(obj.name).transform;
                // newRoot.SetParent(Root);
                // Preload(obj, 1, newRoot, true); 
                return null;
            }
            return Pools[obj.GetInstanceID()].SpawnDirectly(obj, pos, rot);
        }

        private static GameUnit Spawn(GameUnit obj, Vector3 pos, Quaternion rot)
        {
            if (!PoolInstanceID.ContainsKey(obj.GetInstanceID()))
            {
                Transform newRoot = new GameObject(obj.name).transform;
                newRoot.SetParent(Root);
                Preload(obj, 1, newRoot, true);
            }

            return PoolInstanceID[obj.GetInstanceID()].Spawn(pos, rot);
        }

        private static GameUnit Spawn(GameUnit obj)
        {
            if (!PoolInstanceID.ContainsKey(obj.GetInstanceID()))
            {
                Transform newRoot = new GameObject(obj.name).transform;
                newRoot.SetParent(Root);
                Preload(obj, 1, newRoot, true);
            }

            return PoolInstanceID[obj.GetInstanceID()].Spawn();
        }
        public static void Despawn(this GameUnit obj)
        {
            if (obj.gameObject.activeSelf)
            {
                if (Pools.ContainsKey(obj.GetInstanceID()))
                    Pools[obj.GetInstanceID()].DeSpawn(obj);
                else
                    Object.Destroy(obj.gameObject);
            }
        }

        public static void Release(this GameUnit obj)
        {
            if (Pools.ContainsKey(obj.GetInstanceID()))
            {
                Pools[obj.GetInstanceID()].Release();
                Pools.Remove(obj.GetInstanceID());
            }
            else
            {
                Object.Destroy(obj.gameObject);
            }
        }

        public static void ReleaseImmediate(this GameUnit obj)
        {
            if (Pools.ContainsKey(obj.GetInstanceID()))
            {
                Pools[obj.GetInstanceID()].Release();
                Pools.Remove(obj.GetInstanceID());
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }

        public static void Collect(this GameUnit obj)
        {
            if (PoolInstanceID.ContainsKey(obj.GetInstanceID()))
                PoolInstanceID[obj.GetInstanceID()].Collect();
        }

        public static void CollectAll()
        {
            foreach (KeyValuePair<int, Pool> item in PoolInstanceID)
                if (item.Value.IsCollect)
                    item.Value.Collect();
        }

        private static GameUnit GetGameUnitByType(PoolType poolType)
        {
            if (_gameUnitResources == null || _gameUnitResources.Length == 0)
                _gameUnitResources = Resources.LoadAll<GameUnit>(PATH);

            if (!PoolTypes.ContainsKey(poolType) || PoolTypes[poolType] == null)
            {
                GameUnit unit = null;

                for (int i = 0; i < _gameUnitResources.Length; i++)
                    if (_gameUnitResources[i].PoolType == poolType)
                    {
                        unit = _gameUnitResources[i];
                        break;
                    }

                PoolTypes.Add(poolType, unit);
            }

            return PoolTypes[poolType];
        }

        private class Pool
        {
            //collect obj active inGame
            private readonly List<GameUnit> _active;

            private readonly LinkedList<GameUnit> _inactive;
            private readonly int _mAmount;

            private readonly bool _mClamp;

            private readonly Transform _mSRoot;

            // The prefab that we are pooling
            private readonly GameUnit _prefab;

            // Constructor
            public Pool(GameUnit prefab, int initialQty, Transform parent, bool collect, bool clamp)
            {
                _inactive = new LinkedList<GameUnit>();
                _mSRoot = parent;
                _prefab = prefab;
                IsCollect = collect;
                _mClamp = clamp;
                if (IsCollect) _active = new List<GameUnit>();
                if (_mClamp) _mAmount = initialQty;
            }

            public bool IsCollect { get; }

            public int Count => _inactive.Count;

            // Spawn an object from our pool
            public GameUnit Spawn(Vector3 pos, Quaternion rot)
            {
                GameUnit obj = Spawn();

                obj.Tf.SetPositionAndRotation(pos, rot);

                return obj;
            }

            public GameUnit Spawn()
            {
                GameUnit obj;
                if (_inactive.Count == 0)
                {
                    obj = Object.Instantiate(_prefab, _mSRoot);
                    if (!Pools.ContainsKey(obj.GetInstanceID()))
                        Pools.Add(obj.GetInstanceID(), this);
                }
                else
                {
                    // Grab the first object in the inactive array
                    obj = _inactive.First.Value;
                    _inactive.RemoveFirst();

                    if (obj == null) return Spawn();
                }

                if (IsCollect) _active.Add(obj);
                if (_mClamp && _active.Count >= _mAmount) DeSpawn(_active[0]);

                obj.gameObject.SetActive(true);

                return obj;
            }

            public GameUnit SpawnDirectly(GameUnit obj, Vector3 pos, Quaternion rot)
            {
                SpawnDirectly(obj).Tf.SetPositionAndRotation(pos, rot);
                return obj;
            }

            private GameUnit SpawnDirectly(GameUnit gameUnit)
            {
                // check if unit contain in queue
                if (!_inactive.Contains(gameUnit))
                {
                    // NOTE: Comment it because we know that this unit is already in queue
                    // gameUnit = Object.Instantiate(gameUnit, _mSRoot);
                    //
                    // if (!Pools.ContainsKey(gameUnit.GetInstanceID()))
                    //     Pools.Add(gameUnit.GetInstanceID(), this);
                }
                else
                {
                    // remove this unit from queue
                    _inactive.Remove(gameUnit);
                }
                if (IsCollect) _active.Add(gameUnit);
                if (_mClamp && _active.Count >= _mAmount) DeSpawn(_active[0]);

                gameUnit.gameObject.SetActive(true);

                return gameUnit;
            }

            // Return an object to the inactive pool.
            public void DeSpawn(GameUnit obj)
            {
                if (obj != null /*&& !inactive.Contains(obj)*/)
                {
                    obj.gameObject.SetActive(false);
                    // _inactive.Enqueue(obj);
                    _inactive.AddLast(obj);
                }

                if (IsCollect) _active.Remove(obj);
            }

            public void Clamp(int amount)
            {
                while (_inactive.Count > amount)
                {
                    // GameUnit go = _inactive.Dequeue();
                    GameUnit go = _inactive.First.Value;
                    _inactive.RemoveFirst();
                    Object.DestroyImmediate(go);
                }
            }

            public void Release()
            {
                while (_inactive.Count > 0)
                {
                    // GameUnit go = _inactive.Dequeue();
                    GameUnit go = _inactive.First.Value;
                    _inactive.RemoveFirst();
                    Object.DestroyImmediate(go);
                }

                _inactive.Clear();
            }

            public void Collect()
            {
                while (_active.Count > 0) DeSpawn(_active[0]);
            }
        }
    }

    [Serializable]
    public struct PoolAmount
    {
        [Header("-- Pool Amount --")] public Transform root;

        public GameUnit prefab;
        public int amount;
        public bool collect;
        public bool clamp;
    }

    public enum PoolType
    {
        NONE = -1,
        PLAYER = 0,
        SURFACE = 1,
        DYNAMIC_RICE_UNIT = 2,
        RICE_CLUSTER = 3,
        KNIFE_UNIT = 4,
        KNIFE_CLUSTER = 5,
        HOLDER_SLOT = 6,
        STATIC_RICE_UNIT = 7,
        HOLDER_SLOT_CLUSTER = 8,
        GRID_DEBUG_TEXT = 100,
        FIRE_BALL_EXPLOSION = 101,
        EXPLOSION_THROW = 102,
        BULLET = 103,
        SOUND_UNIT = 104,
    }

    public enum VFX
    {
        NONE = -1,
        DUST = 0,
        COMPLETE = 1,
        KNIFE_CUT = 2,
        SOCOLA_BREAK = 3,
        STAR_TRAIL = 4,
    }
}
