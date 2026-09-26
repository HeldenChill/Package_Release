using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Hung.UI
{
    /// <summary>Loads prefab leases without imposing an address convention on the UI package.</summary>
    public interface IUIAsyncPrefabProvider
    {
        /// <summary>Load a prefab and return a lease that keeps its source asset alive.</summary>
        Task<UIPrefabLease> LoadAsync(string address);
    }

    /// <summary>Loaded prefab plus its idempotent release action.</summary>
    public sealed class UIPrefabLease : IDisposable
    {
        private Action release;

        /// <summary>Prefab GameObject retained by this lease.</summary>
        public GameObject Prefab { get; }

        /// <summary>Create a lease around a prefab and its release action.</summary>
        public UIPrefabLease(GameObject prefab, Action release)
        {
            Prefab = prefab;
            this.release = release;
        }

        /// <summary>Release the loaded source at most once.</summary>
        public void Dispose()
        {
            Action action = release;
            release = null;
            action?.Invoke();
        }
    }
}
