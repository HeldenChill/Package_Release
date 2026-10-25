using System;
using System.Collections.Generic;

namespace Hung.Base.Composition
{
    public enum FeatureState
    {
        Registered = 0,
        Initializing,
        Ready,
        Failed,
        Blocked,
        Stopped
    }

    public readonly struct FeatureStartResult
    {
        public FeatureStartResult(bool success, string diagnosticCode = null)
        {
            Success = success;
            DiagnosticCode = diagnosticCode;
        }

        public bool Success { get; }
        public string DiagnosticCode { get; }
    }

    public sealed class FeatureRegistration
    {
        public FeatureRegistration(string id, bool required, IReadOnlyList<string> dependencies,
            Func<FeatureStartResult> initialize, Action shutdown)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Feature ID cannot be blank.", nameof(id));
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            Initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
            Shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));

            var copy = new string[dependencies.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < copy.Length; i++)
            {
                string dependency = dependencies[i];
                if (string.IsNullOrWhiteSpace(dependency))
                    throw new ArgumentException("Dependency ID cannot be blank.", nameof(dependencies));
                if (string.Equals(dependency, id, StringComparison.Ordinal))
                    throw new ArgumentException("A feature cannot depend on itself.", nameof(dependencies));
                if (!seen.Add(dependency))
                    throw new ArgumentException("Duplicate dependency ID.", nameof(dependencies));
                copy[i] = dependency;
            }

            Id = id;
            Required = required;
            Dependencies = Array.AsReadOnly(copy);
        }

        public string Id { get; }
        public bool Required { get; }
        public IReadOnlyList<string> Dependencies { get; }
        internal Func<FeatureStartResult> Initialize { get; }
        internal Action Shutdown { get; }
    }

    public sealed class FeatureReport
    {
        internal FeatureReport(string id, FeatureState state, string diagnosticCode, string dependencyId)
        {
            Id = id;
            State = state;
            DiagnosticCode = diagnosticCode;
            DependencyId = dependencyId;
        }

        public string Id { get; }
        public FeatureState State { get; }
        public string DiagnosticCode { get; }
        public string DependencyId { get; }
    }

    public sealed class FeatureDiagnostic
    {
        internal FeatureDiagnostic(string featureId, string code, string exceptionDetails = null)
        {
            FeatureId = featureId;
            Code = code;
            ExceptionDetails = exceptionDetails;
        }

        public string FeatureId { get; }
        public string Code { get; }
        public string ExceptionDetails { get; }
    }

    public sealed class FeatureBootReport
    {
        internal FeatureBootReport(bool productReady, FeatureReport[] features, FeatureDiagnostic[] diagnostics)
        {
            ProductReady = productReady;
            Features = Array.AsReadOnly((FeatureReport[])features.Clone());
            Diagnostics = Array.AsReadOnly((FeatureDiagnostic[])diagnostics.Clone());
        }

        public bool ProductReady { get; }
        public IReadOnlyList<FeatureReport> Features { get; }
        public IReadOnlyList<FeatureDiagnostic> Diagnostics { get; }
    }
}
