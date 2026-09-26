using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI.Scoping
{
    /// <summary>
    /// Owns the live scope path and closes the canvases that path change invalidates.
    ///
    /// Knows nothing about phases, scenes, or gameplay — Layer 5 pushes the path down through
    /// <see cref="Enter"/>. Replaces the hand-written cleanup at each transition site that
    /// produced BUG-0288, BUG-0287 and BUG-0283.
    ///
    /// Source design: PVM .cursor/plans/ui-scope-hierarchy-popup-lifecycle.md
    /// </summary>
    public sealed class UiScopeRegistry
    {
        static UiScopeRegistry _instance;

        /// <summary>
        /// Plain object, not a MonoBehaviour: it holds no scene state and must survive scene
        /// loads. Tests construct their own instance so they never touch this one.
        /// </summary>
        public static UiScopeRegistry Instance => _instance ??= new UiScopeRegistry();

        // Bound path per open canvas — the path captured when it registered, which is NOT
        // necessarily its declared ceiling (decision 7: capture at open).
        readonly Dictionary<IScopedUi, UiScopePath> _bound = new();

        public UiScopePath Current { get; private set; } = UiScopePath.None;

        /// <summary>
        /// Makes <paramref name="path"/> the live scope and closes every registered canvas whose
        /// bound scope it left.
        /// </summary>
        public void Enter(UiScopePath path)
        {
            if (path == Current) return;
            Current = path;

            var closing = new List<IScopedUi>();
            foreach (KeyValuePair<IScopedUi, UiScopePath> entry in _bound)
            {
                if (entry.Key.Scope.ShouldCloseOn(entry.Value, path)) closing.Add(entry.Key);
            }

            // Iterate a snapshot: OnScopeExit reaches UICanvas.Close, which unregisters, and
            // mutating _bound mid-foreach would throw and leave the rest of the popups open.
            for (int i = 0; i < closing.Count; i++)
            {
                closing[i].OnScopeExit();
            }
        }

        /// <summary>
        /// Binds an opening canvas to the live path. Re-registering an already-open canvas
        /// re-binds it, which is what makes a reopened popup belong to the phase it reopened in.
        /// </summary>
        public void Register(IScopedUi canvas)
        {
            if (canvas == null) return;

            UiScope scope = canvas.Scope ?? UiScope.Global;
            UiScopePath bound = scope.Bind(Current);

            // The live path is outside this canvas's ceiling, so it could not bind to it. That is
            // a mis-tag: the canvas is opening somewhere it declared it does not belong. Warn
            // rather than throw — an un-migrated screen must still work.
            if (!scope.IsGlobal && !Current.IsAtOrUnder(scope.Ceiling))
            {
                Debug.LogWarning(
                    $"[UiScope] {canvas} declares scope '{scope.Ceiling}' but opened at '{Current}'. " +
                    $"Binding to '{bound}' — it will close on the next transition.");
            }

            _bound[canvas] = bound;
        }

        public void Unregister(IScopedUi canvas)
        {
            if (canvas == null) return;
            _bound.Remove(canvas);
        }
    }
}
