using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    /// <summary>
    /// Main-thread registry for framework service contracts. Owners are implementation
    /// assembly names. Does not dispose services or install plugin implementations.
    /// </summary>
    public static class ServiceSlots
    {
        private sealed class Entry
        {
            internal readonly object Service;
            internal readonly string Owner;

            internal Entry(object service, string owner)
            {
                Service = service;
                Owner = owner;
            }
        }

        private static readonly Dictionary<Type, Entry> entries = new Dictionary<Type, Entry>();
        private static readonly Dictionary<Type, object> fallbacks = new Dictionary<Type, object>();

        static ServiceSlots() => ResetForTests();

        /// <summary>Installs one default per contract per session, without claiming ownership.</summary>
        public static void SetFallback<T>(T fallback) where T : class
        {
            if (ReferenceEquals(fallback, null)) throw new ArgumentNullException(nameof(fallback));
            if (fallbacks.TryGetValue(typeof(T), out var existing))
            {
                if (ReferenceEquals(existing, fallback)) return;
                throw new InvalidOperationException("A different fallback is already installed for " + typeof(T).Name + ".");
            }
            fallbacks.Add(typeof(T), fallback);
        }

        /// <summary>
        /// Registers a non-null instance. The same owner may replace it; another
        /// implementation assembly is refused without changing the current value.
        /// </summary>
        public static ServiceRegistration Register<T>(T service) where T : class
        {
            if (ReferenceEquals(service, null))
            {
                Debug.LogError($"[ServiceSlots] Cannot register null for {typeof(T).Name}.");
                return ServiceRegistration.RefusedNull;
            }

            var contract = typeof(T);
            string owner = service.GetType().Assembly.GetName().Name;
            if (entries.TryGetValue(contract, out var current))
            {
                if (ReferenceEquals(current.Service, service))
                    return ServiceRegistration.AlreadyRegistered;
                if (!string.Equals(current.Owner, owner, StringComparison.Ordinal))
                {
                    Debug.LogError($"[ServiceSlots] {contract.Name} is owned by {current.Owner}; refused registration from {owner}.");
                    return ServiceRegistration.RefusedDuplicateOwner;
                }
                entries[contract] = new Entry(service, owner);
                return ServiceRegistration.Replaced;
            }

            entries.Add(contract, new Entry(service, owner));
            return ServiceRegistration.Registered;
        }

        /// <summary>Removes only the exact currently registered instance.</summary>
        public static bool Unregister<T>(T service) where T : class
        {
            if (ReferenceEquals(service, null) || !entries.TryGetValue(typeof(T), out var current)
                || !ReferenceEquals(current.Service, service))
                return false;
            return entries.Remove(typeof(T));
        }

        /// <summary>Returns the real registration, then its default, otherwise null.</summary>
        public static T Get<T>() where T : class
        {
            if (TryGet<T>(out var service)) return service;
            return fallbacks.TryGetValue(typeof(T), out var fallback) ? (T)fallback : null;
        }

        /// <summary>Returns true only when a contract has an explicit registration.</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (entries.TryGetValue(typeof(T), out var entry))
            {
                service = (T)entry.Service;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>Returns the implementation assembly name, or null for an empty slot.</summary>
        public static string OwnerOf<T>() where T : class
        {
            return entries.TryGetValue(typeof(T), out var entry) ? entry.Owner : null;
        }

        /// <summary>Clears registry references and owners without disposing services.</summary>
        public static void ResetForTests()
        {
            entries.Clear();
            fallbacks.Clear();
            ServiceSlotFallbacks.Install();
        }

        private static void ResetForPlay() => ResetForTests();
    }
}
