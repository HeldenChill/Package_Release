using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    public class CanvasRegistry
    {
        private readonly IUIPrefabProvider provider;
        private readonly RectTransform parent;
        private readonly Dictionary<Type, UICanvas> instances = new();

        public CanvasRegistry(IUIPrefabProvider provider, RectTransform parent)
        {
            this.provider = provider;
            this.parent = parent;
        }

        public IReadOnlyCollection<UICanvas> All => instances.Values;

        public bool IsLoaded<T>() where T : UICanvas
        {
            return instances.TryGetValue(typeof(T), out UICanvas ui) && ui != null;
        }

        /// <summary>Return an already instantiated canvas without invoking the Resources provider.</summary>
        public T GetLoaded<T>() where T : UICanvas => IsLoaded<T>() ? instances[typeof(T)] as T : null;

        /// <summary>Instantiate and cache a prefab acquired by an external provider.</summary>
        public T RegisterPrefab<T>(T prefab) where T : UICanvas
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            T existing = GetLoaded<T>();
            if (existing != null) return existing;
            T instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            instances[typeof(T)] = instance;
            return instance;
        }

        public T Get<T>() where T : UICanvas
        {
            if (IsLoaded<T>()) return instances[typeof(T)] as T;

            T prefab = provider.GetPrefab<T>();
            if (prefab == null)
                throw new InvalidOperationException($"No UI prefab for {typeof(T).Name}");

            return RegisterPrefab(prefab);
        }

        public bool Contains(UICanvas ui)
        {
            return instances.ContainsValue(ui);
        }

        public void Remove(UICanvas ui)
        {
            Type key = null;
            foreach (KeyValuePair<Type, UICanvas> kv in instances)
                if (kv.Value == ui) { key = kv.Key; break; }
            if (key != null) instances.Remove(key);
        }

        public void Clear()
        {
            instances.Clear();
        }
    }
}
