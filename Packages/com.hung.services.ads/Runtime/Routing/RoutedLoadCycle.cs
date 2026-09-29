using System;
using System.Collections.Generic;

namespace Hung.Ads
{
    /// <summary>
    /// Chain/parallel load engine shared by routed full-screen providers. Route 0 is the default
    /// route and raises DefaultLoaded or DefaultFailed once per Start; other routes load silently.
    /// Re-entrant: a handler may call Start again from inside an event.
    /// </summary>
    internal sealed class RoutedLoadCycle<T> where T : class
    {
        sealed class Track
        {
            public AdsRouteMode Mode;
            public IReadOnlyList<T> Children;
            public int Next;
            public readonly HashSet<T> Failed = new HashSet<T>();
            public bool Finished;
        }

        readonly Func<T, bool> isReady;
        readonly Func<T, bool> isLoading;
        readonly Action<T> load;
        readonly List<Track> tracks = new List<Track>();
        int generation;
        bool defaultPending;

        public event Action DefaultLoaded;
        public event Action DefaultFailed;

        public RoutedLoadCycle(Func<T, bool> isReady, Func<T, bool> isLoading, Action<T> load)
        {
            this.isReady = isReady;
            this.isLoading = isLoading;
            this.load = load;
        }

        /// <summary>True until the default route has reported loaded or failed.</summary>
        public bool IsRunning => defaultPending;

        public void Start(IReadOnlyList<(AdsRouteMode mode, IReadOnlyList<T> children)> routes)
        {
            generation++;
            tracks.Clear();
            defaultPending = true;
            foreach (var r in routes) tracks.Add(new Track { Mode = r.mode, Children = r.children });
            if (tracks.Count == 0) tracks.Add(new Track { Mode = AdsRouteMode.Chain, Children = Array.Empty<T>() });

            int gen = generation;
            foreach (var t in tracks.ToArray())
            {
                Kick(t);
                if (gen != generation) return;
            }
        }

        public void OnChildLoaded(T child)
        {
            int gen = generation;
            foreach (var t in tracks.ToArray())
            {
                if (t.Finished || !Contains(t, child)) continue;
                Finish(t, true);
                if (gen != generation) return;
            }
        }

        public void OnChildLoadFailed(T child)
        {
            int gen = generation;
            foreach (var t in tracks.ToArray())
            {
                if (t.Finished) continue;
                if (t.Mode == AdsRouteMode.Chain)
                {
                    if (t.Next >= t.Children.Count || !ReferenceEquals(t.Children[t.Next], child)) continue;
                    t.Next++;
                    Kick(t);
                }
                else
                {
                    if (!Contains(t, child) || !t.Failed.Add(child)) continue;
                    if (t.Failed.Count >= t.Children.Count) Finish(t, false);
                }
                if (gen != generation) return;
            }
        }

        void Kick(Track t)
        {
            if (t.Mode == AdsRouteMode.Chain)
            {
                if (t.Next >= t.Children.Count) { Finish(t, false); return; }
                var c = t.Children[t.Next];
                if (isReady(c)) { Finish(t, true); return; }
                if (!isLoading(c)) load(c);
                return;
            }

            if (t.Children.Count == 0) { Finish(t, false); return; }
            foreach (var c in t.Children)
            {
                if (isReady(c)) { Finish(t, true); return; }
            }
            int gen = generation;
            foreach (var c in t.Children)
            {
                if (t.Finished || gen != generation) return;
                if (!isLoading(c)) load(c);
            }
        }

        void Finish(Track t, bool loaded)
        {
            if (t.Finished) return;
            t.Finished = true;
            if (tracks.Count == 0 || !ReferenceEquals(tracks[0], t) || !defaultPending) return;
            defaultPending = false;
            if (loaded) DefaultLoaded?.Invoke();
            else DefaultFailed?.Invoke();
        }

        static bool Contains(Track t, T child)
        {
            foreach (var c in t.Children) if (ReferenceEquals(c, child)) return true;
            return false;
        }
    }
}
