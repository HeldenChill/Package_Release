using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hung.UI
{
    /// <summary>Deduplicates addressed canvas loads and retains prefab leases until disposed.</summary>
    public sealed class AsyncCanvasRegistry : IDisposable
    {
        private sealed class Request<T> where T : UICanvas
        {
            public readonly string Address;
            public readonly TaskCompletionSource<UIAcquireResult<T>> Completion =
                new TaskCompletionSource<UIAcquireResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);

            public Request(string address) => Address = address;
        }

        private readonly CanvasRegistry registry;
        private readonly IUIAsyncPrefabProvider provider;
        private readonly Dictionary<Type, object> requests = new Dictionary<Type, object>();
        private readonly Dictionary<Type, string> addresses = new Dictionary<Type, string>();
        private readonly Dictionary<Type, UIPrefabLease> retained = new Dictionary<Type, UIPrefabLease>();
        private bool disposed;

        /// <summary>Create an acquisition cache around an existing canvas registry and provider.</summary>
        public AsyncCanvasRegistry(CanvasRegistry registry, IUIAsyncPrefabProvider provider)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>Acquire one canvas per type and address, sharing concurrent loads.</summary>
        public Task<UIAcquireResult<T>> AcquireAsync<T>(string address) where T : UICanvas
        {
            if (disposed) return Task.FromResult(UIAcquireResult<T>.Failed(address,
                UIAcquireFailure.ManagerDestroyed, "Acquisition registry was disposed."));
            if (string.IsNullOrWhiteSpace(address)) return Task.FromResult(UIAcquireResult<T>.Failed(address,
                UIAcquireFailure.InvalidAddress, "A nonempty address is required."));

            Type type = typeof(T);
            if (requests.TryGetValue(type, out object pending))
            {
                Request<T> active = (Request<T>)pending;
                return active.Address == address ? active.Completion.Task :
                    Task.FromResult(UIAcquireResult<T>.Failed(address, UIAcquireFailure.AddressMismatch,
                        $"{type.Name} is already loading from '{active.Address}'."));
            }

            T loaded = registry.GetLoaded<T>();
            if (loaded != null)
            {
                return Task.FromResult(addresses.TryGetValue(type, out string loadedAddress) && loadedAddress == address
                    ? UIAcquireResult<T>.Success(address, loaded)
                    : UIAcquireResult<T>.Failed(address, UIAcquireFailure.AddressMismatch,
                        $"{type.Name} is already loaded from a different source."));
            }

            if (retained.TryGetValue(type, out UIPrefabLease staleLease))
            {
                retained.Remove(type);
                addresses.Remove(type);
                staleLease.Dispose();
            }

            var request = new Request<T>(address);
            requests[type] = request;
            _ = PerformAsync(request);
            return request.Completion.Task;
        }

        private async Task PerformAsync<T>(Request<T> request) where T : UICanvas
        {
            UIPrefabLease lease = null;
            bool retainedLease = false;
            try
            {
                lease = await provider.LoadAsync(request.Address);
                if (disposed)
                {
                    request.Completion.TrySetResult(UIAcquireResult<T>.Failed(request.Address,
                        UIAcquireFailure.ManagerDestroyed, "Acquisition registry was disposed during load."));
                    return;
                }
                if (registry.GetLoaded<T>() != null)
                {
                    request.Completion.TrySetResult(UIAcquireResult<T>.Failed(request.Address,
                        UIAcquireFailure.AddressMismatch, "Canvas was registered from another source during load."));
                    return;
                }
                T prefab = lease?.Prefab == null ? null : lease.Prefab.GetComponent<T>();
                if (prefab == null)
                {
                    request.Completion.TrySetResult(UIAcquireResult<T>.Failed(request.Address,
                        UIAcquireFailure.MissingCanvasComponent, "Loaded prefab has no requested canvas component."));
                    return;
                }

                T instance;
                try { instance = registry.RegisterPrefab(prefab); }
                catch (Exception ex)
                {
                    request.Completion.TrySetResult(UIAcquireResult<T>.Failed(request.Address,
                        UIAcquireFailure.InstantiationFailed, ex.Message));
                    return;
                }
                addresses[typeof(T)] = request.Address;
                retained[typeof(T)] = lease;
                retainedLease = true;
                request.Completion.TrySetResult(UIAcquireResult<T>.Success(request.Address, instance));
            }
            catch (Exception ex)
            {
                request.Completion.TrySetResult(UIAcquireResult<T>.Failed(request.Address,
                    disposed ? UIAcquireFailure.ManagerDestroyed : UIAcquireFailure.LoadFailed, ex.Message));
            }
            finally
            {
                if (!retainedLease) lease?.Dispose();
                if (requests.TryGetValue(typeof(T), out object active) && ReferenceEquals(active, request))
                    requests.Remove(typeof(T));
            }
        }

        /// <summary>Release every retained prefab source exactly once.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (UIPrefabLease lease in retained.Values) lease.Dispose();
            retained.Clear();
        }
    }
}
