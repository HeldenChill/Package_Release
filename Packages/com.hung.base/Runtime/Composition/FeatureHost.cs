using System;
using System.Collections.Generic;

namespace Hung.Base.Composition
{
    /// <summary>Instance-owned synchronous lifecycle. No discovery or service resolution.</summary>
    public sealed class FeatureHost : IDisposable
    {
        private sealed class Node
        {
            public FeatureRegistration Registration;
            public FeatureState State = FeatureState.Registered;
            public string Code;
            public string DependencyId;
            public bool CleanupAttempted;
        }

        private readonly List<Node> nodes = new List<Node>();
        private readonly Dictionary<string, Node> byId = new Dictionary<string, Node>(StringComparer.Ordinal);
        private readonly List<Node> started = new List<Node>();
        private readonly List<FeatureDiagnostic> diagnostics = new List<FeatureDiagnostic>();
        private bool frozen;
        private bool operating;
        private bool disposed;
        private bool completed;
        private bool fatal;
        private FeatureBootReport startupReport;

        public void Register(FeatureRegistration feature)
        {
            EnsureIdle();
            EnsureNotDisposed();
            if (frozen) throw new InvalidOperationException("Feature registration is frozen.");
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            if (byId.ContainsKey(feature.Id)) throw new ArgumentException("Duplicate feature ID.", nameof(feature));
            var node = new Node { Registration = feature };
            byId.Add(feature.Id, node);
            nodes.Add(node);
        }

        public FeatureBootReport Start()
        {
            EnsureIdle();
            EnsureNotDisposed();
            if (startupReport != null) return startupReport;
            frozen = true;
            operating = true;
            try
            {
                Preflight();
                if (RequiredBranchFailed()) fatal = true;

                while (!fatal)
                {
                    Node next = FindEligible();
                    if (next == null) break;
                    StartNode(next);
                    PropagateBlocked();
                    if (RequiredBranchFailed()) fatal = true;
                }

                if (fatal) Unwind();
                completed = true;
                startupReport = GetReport();
                return startupReport;
            }
            finally
            {
                operating = false;
            }
        }

        public FeatureState GetState(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (!byId.TryGetValue(id, out Node node)) throw new KeyNotFoundException("Feature is not selected: " + id);
            return node.State;
        }

        public FeatureBootReport GetReport()
        {
            var rows = new FeatureReport[nodes.Count];
            bool ready = completed && !fatal && !disposed;
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                rows[i] = new FeatureReport(node.Registration.Id, node.State, node.Code, node.DependencyId);
                if (node.Registration.Required && node.State != FeatureState.Ready) ready = false;
            }
            return new FeatureBootReport(ready, rows, diagnostics.ToArray());
        }

        public void Dispose()
        {
            EnsureIdle();
            if (disposed) return;
            operating = true;
            disposed = true;
            try
            {
                Unwind();
            }
            finally
            {
                operating = false;
            }
        }

        private void Preflight()
        {
            // A node is a cycle member iff following its dependencies reaches itself.
            // Small explicit boot graphs favor this bounded, transparent check over a
            // general graph service. No callbacks run during graph inspection.
            foreach (Node node in nodes)
            {
                if (Reaches(node, node, new HashSet<string>(StringComparer.Ordinal)))
                    Mark(node, FeatureState.Failed, "FEATURE_DEPENDENCY_CYCLE");
            }

            foreach (Node node in nodes)
            {
                if (node.State != FeatureState.Registered) continue;
                foreach (string dependency in node.Registration.Dependencies)
                {
                    if (byId.ContainsKey(dependency)) continue;
                    Mark(node, FeatureState.Blocked, "FEATURE_DEPENDENCY_MISSING", dependency);
                    break;
                }
            }
            PropagateBlocked();
        }

        private bool Reaches(Node current, Node target, HashSet<string> visited)
        {
            if (!visited.Add(current.Registration.Id)) return false;
            foreach (string dependency in current.Registration.Dependencies)
            {
                if (!byId.TryGetValue(dependency, out Node next)) continue;
                if (ReferenceEquals(next, target) || Reaches(next, target, visited)) return true;
            }
            return false;
        }

        private void PropagateBlocked()
        {
            bool changed;
            do
            {
                changed = false;
                foreach (Node node in nodes)
                {
                    if (node.State != FeatureState.Registered) continue;
                    foreach (string dependency in node.Registration.Dependencies)
                    {
                        if (!byId.TryGetValue(dependency, out Node required)) continue;
                        if (required.State != FeatureState.Failed && required.State != FeatureState.Blocked) continue;
                        Mark(node, FeatureState.Blocked, "FEATURE_DEPENDENCY_NOT_READY", dependency);
                        changed = true;
                        break;
                    }
                }
            } while (changed);
        }

        private Node FindEligible()
        {
            foreach (Node node in nodes)
            {
                if (node.State != FeatureState.Registered) continue;
                bool ready = true;
                foreach (string dependency in node.Registration.Dependencies)
                {
                    if (!byId.TryGetValue(dependency, out Node required) || required.State != FeatureState.Ready)
                    {
                        ready = false;
                        break;
                    }
                }
                if (ready) return node;
            }
            return null;
        }

        private void StartNode(Node node)
        {
            node.State = FeatureState.Initializing;
            try
            {
                FeatureStartResult result = node.Registration.Initialize();
                if (result.Success)
                {
                    node.State = FeatureState.Ready;
                    started.Add(node);
                    return;
                }
                string code = string.IsNullOrWhiteSpace(result.DiagnosticCode)
                    ? "FEATURE_INITIALIZATION_FAILED" : result.DiagnosticCode;
                Mark(node, FeatureState.Failed, code);
            }
            catch (Exception exception)
            {
                Mark(node, FeatureState.Failed, "FEATURE_INITIALIZATION_EXCEPTION", exception: exception);
            }
            Cleanup(node);
        }

        private bool RequiredBranchFailed()
        {
            foreach (Node node in nodes)
                if (node.Registration.Required &&
                    (node.State == FeatureState.Failed || node.State == FeatureState.Blocked)) return true;
            return false;
        }

        private void Mark(Node node, FeatureState state, string code, string dependency = null, Exception exception = null)
        {
            node.State = state;
            node.Code = code;
            node.DependencyId = dependency;
            diagnostics.Add(new FeatureDiagnostic(node.Registration.Id, code, DescribeException(exception)));
        }

        private void Cleanup(Node node)
        {
            if (node.CleanupAttempted) return;
            node.CleanupAttempted = true;
            try
            {
                node.Registration.Shutdown();
            }
            catch (Exception exception)
            {
                fatal = true;
                diagnostics.Add(new FeatureDiagnostic(node.Registration.Id,
                    "FEATURE_SHUTDOWN_EXCEPTION", DescribeException(exception)));
            }
            finally
            {
                if (node.State == FeatureState.Ready) node.State = FeatureState.Stopped;
            }
        }

        private static string DescribeException(Exception exception)
        {
            if (exception == null) return null;
            try
            {
                return exception.ToString();
            }
            catch (Exception)
            {
                // Exception.ToString and Message are virtual; diagnostics must not
                // interrupt partial cleanup or the remaining reverse unwind.
                return exception.GetType().FullName + " (exception details unavailable)";
            }
        }

        private void Unwind()
        {
            for (int i = started.Count - 1; i >= 0; i--) Cleanup(started[i]);
            started.Clear();
        }

        private void EnsureIdle()
        {
            if (operating) throw new InvalidOperationException("Feature host operation is already in progress.");
        }

        private void EnsureNotDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(FeatureHost));
        }
    }
}
