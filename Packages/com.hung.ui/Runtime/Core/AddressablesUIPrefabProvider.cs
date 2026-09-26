using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hung.UI
{
    /// <summary>Addressables-backed prefab provider that retains each successful load handle.</summary>
    public sealed class AddressablesUIPrefabProvider : IUIAsyncPrefabProvider
    {
        /// <summary>Load a prefab by its caller-supplied Addressables address.</summary>
        public async Task<UIPrefabLease> LoadAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required.", nameof(address));
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
            try
            {
                GameObject prefab = await handle.Task;
                if (handle.Status != AsyncOperationStatus.Succeeded || prefab == null)
                    throw new InvalidOperationException($"Addressables did not load UI prefab '{address}'.");
                return new UIPrefabLease(prefab, () => Addressables.Release(handle));
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
        }
    }
}
