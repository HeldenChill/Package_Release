using System;
using UnityEngine;

namespace Hung.Base
{
    /// <summary>Prevents a second application-scoped scene owner from starting side effects.</summary>
    public static class ServiceSlotLifecycle
    {
        /// <summary>Checks a scene owner before initialization; refuses and disables duplicate components.</summary>
        public static bool CanInitialize<T>(T service, Behaviour owner) where T : class
        {
            if (ReferenceEquals(service, null)) throw new ArgumentNullException(nameof(service));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!ServiceSlots.TryGet<T>(out var current) || ReferenceEquals(current, service)) return true;
            if (!string.Equals(ServiceSlots.OwnerOf<T>(), service.GetType().Assembly.GetName().Name, StringComparison.Ordinal))
                ServiceSlots.Register<T>(service);
            else
                Debug.LogError("[ServiceSlots] Active scene owner already exists for " + typeof(T).Name + ".");
            owner.enabled = false;
            return false;
        }
    }
}
