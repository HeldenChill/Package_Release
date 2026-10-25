using UnityEngine;

namespace Hung.DesignPattern.ScopedPooling
{
    /// <summary>Scope-owned runtime ticking; never promotes the host or its root to persistent lifetime.</summary>
    [AddComponentMenu("")]
    public sealed class PoolScopeRunner : MonoBehaviour
    {
        private PoolScope scope;

        internal void Bind(PoolScope value) { scope = value; }

        private void LateUpdate() => scope?.Tick(Time.deltaTime, Time.unscaledDeltaTime);

        private void OnDestroy() { var value = scope; scope = null; value?.Dispose(); }

        internal void Unbind() { scope = null; }
    }

    public sealed partial class PoolScope
    {
        private PoolScopeRunner runner;

        partial void AttachRunner()
        {
            if (!Application.isPlaying) return;
            var container = new GameObject("ScopedPoolRunner");
            container.transform.SetParent(root, false);
            runner = container.AddComponent<PoolScopeRunner>();
            runner.Bind(this);
        }

        partial void DetachRunner()
        {
            if (runner == null) return;
            runner.Unbind();
            ScopedPoolGroup.DestroyOwned(runner.gameObject);
            runner = null;
        }
    }
}
