using System;
using System.Collections.Generic;

namespace Hung.UI.Scoping
{
    /// <summary>
    /// A canvas's declared lifetime. The declared path is a CEILING, not the final answer: the
    /// path a canvas actually lives at is captured from the live path when it opens
    /// (<see cref="Bind"/>), so the same popup opened in the trader phase and in the defense
    /// phase closes with whichever phase it was opened in.
    ///
    /// Immutable — <see cref="Survives"/> returns a new instance, so a scope declared as a
    /// `=>` property cannot accumulate survivors across reads.
    ///
    /// Source design: PVM .cursor/plans/ui-scope-hierarchy-popup-lifecycle.md (decision 7)
    /// </summary>
    public sealed class UiScope
    {
        static readonly UiScopePath[] NoSurvivors = Array.Empty<UiScopePath>();

        /// <summary>
        /// Never auto-closed by a path change. The default for every canvas that has not declared
        /// a scope, so an untagged canvas behaves exactly as it did before this system existed.
        /// </summary>
        public static UiScope Global { get; } =
            new UiScope(UiScopePath.None, NoSurvivors, true, false);

        readonly UiScopePath[] _survivors;
        readonly bool _captures;

        UiScope(UiScopePath ceiling, UiScopePath[] survivors, bool isGlobal, bool captures)
        {
            Ceiling = ceiling;
            _survivors = survivors;
            IsGlobal = isGlobal;
            _captures = captures;
        }

        public bool IsGlobal { get; }

        /// <summary>The shallowest node this canvas requires. <see cref="None"/> when Global.</summary>
        public UiScopePath Ceiling { get; }

        /// <summary>Paths this canvas stays open in even though they fall outside its bound path.</summary>
        public IReadOnlyList<UiScopePath> Survivors => _survivors;

        /// <summary>
        /// A canvas that LIVES INSIDE <paramref name="ceiling"/> — a popup. It binds to the live
        /// path when it opens, so the same popup opened in two different phases belongs to
        /// whichever one opened it.
        /// </summary>
        public static UiScope At(UiScopePath ceiling) =>
            new UiScope(ceiling, NoSurvivors, false, true);

        /// <summary>
        /// A canvas that DEFINES <paramref name="node"/> — a screen, not a popup. It binds to its
        /// own node exactly and never to a descendant.
        ///
        /// Why this exists: `BaseUICanvas.SelectTab` closes the outgoing tab and opens the
        /// incoming one in the same frame, and the outgoing tab keeps running its hide animation.
        /// `HomeCanvas` therefore opens while the live path is still "Home/Shop". Under
        /// <see cref="At"/> it captured that deeper path, and the path settling back to "Home"
        /// then closed it — the whole screen went blank. A screen must own its node.
        /// </summary>
        public static UiScope Owns(UiScopePath node) =>
            new UiScope(node, NoSurvivors, false, false);

        /// <summary>Returns a new scope with <paramref name="path"/> added. Does not mutate this one.</summary>
        public UiScope Survives(UiScopePath path)
        {
            if (!path.HasValue) return this;

            var extended = new UiScopePath[_survivors.Length + 1];
            Array.Copy(_survivors, extended, _survivors.Length);
            extended[_survivors.Length] = path;
            return new UiScope(Ceiling, extended, IsGlobal, _captures);
        }

        /// <summary>
        /// The path this canvas binds to when opened while the live path is
        /// <paramref name="current"/>. Falls back to the ceiling when the live path is outside it —
        /// that case is a mis-tag, and <c>UiScopeRegistry</c> warns about it.
        /// </summary>
        public UiScopePath Bind(UiScopePath current)
        {
            if (IsGlobal) return UiScopePath.None;
            // An owner is its node — it never binds to a descendant, however deep the live path
            // happens to be while it opens.
            if (!_captures) return Ceiling;
            return current.IsAtOrUnder(Ceiling) ? current : Ceiling;
        }

        /// <summary>
        /// Whether a canvas bound to <paramref name="bound"/> must close now that the live path is
        /// <paramref name="newPath"/>.
        /// </summary>
        public bool ShouldCloseOn(UiScopePath bound, UiScopePath newPath)
        {
            if (IsGlobal) return false;
            if (newPath.IsAtOrUnder(bound)) return false;

            for (int i = 0; i < _survivors.Length; i++)
            {
                if (newPath.IsAtOrUnder(_survivors[i])) return false;
            }

            return true;
        }
    }
}
