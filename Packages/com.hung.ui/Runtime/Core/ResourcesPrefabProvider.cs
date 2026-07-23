using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    public class ResourcesPrefabProvider : IUIPrefabProvider
    {
        private UICanvas[] all;
        private readonly Dictionary<Type, UICanvas> cache = new();

        public T GetPrefab<T>() where T : UICanvas
        {
            if (cache.TryGetValue(typeof(T), out UICanvas hit)) return hit as T;
            all ??= Resources.LoadAll<UICanvas>("UI/");
            for (int i = 0; i < all.Length; i++)
                if (all[i] is T match)
                {
                    cache[typeof(T)] = match;
                    return match;
                }
            return null;
        }
    }
}
