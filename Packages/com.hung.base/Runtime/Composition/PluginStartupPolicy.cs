using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base.Composition
{
    /// <summary>Evaluates the product profile and scoped test overrides without starting plugins.</summary>
    public static class PluginStartupPolicy
    {
        /// <summary>Unique Resources path, without extension.</summary>
        public const string ResourcePath = "HungPluginStartup";
        private static readonly Dictionary<string,List<OverrideScope>> overrides = new Dictionary<string,List<OverrideScope>>(StringComparer.Ordinal);
        private static int generation;
        /// <summary>Evaluates current configuration. Does not cache asset content across sessions.</summary>
        public static bool ShouldStart(string featureId)
        {
            var profiles = Resources.LoadAll<PluginStartupProfile>(ResourcePath);
            if (profiles.Length > 1) throw new InvalidOperationException("Multiple Resources/HungPluginStartup profiles exist.");
            bool result = Evaluate(profiles.Length == 0 ? null : profiles[0], featureId);
            if (overrides.TryGetValue(featureId,out var scopes) && scopes.Count != 0)
                return scopes[scopes.Count-1].Enabled;
            return result;
        }
        /// <summary>Absence enables plugins; malformed configuration fails explicitly.</summary>
        public static bool Evaluate(PluginStartupProfile profile,string featureId)
        {
            if (!PluginStartupProfile.IsValidFeatureId(featureId)) throw new ArgumentException("Invalid plugin feature ID.",nameof(featureId));
            if (profile == null) return true;
            var errors=profile.Validate();
            if (errors.Count != 0) throw new InvalidOperationException(string.Join(" ",errors));
            foreach(var disabled in profile.DisabledFeatureIds)
                if (string.Equals(disabled,featureId,StringComparison.Ordinal)) return false;
            return true;
        }
        /// <summary>Overrides only this new policy for a scoped test; does not bypass legacy policy.</summary>
        public static IDisposable OverrideForTests(string featureId,bool enabled)
        {
            if (!PluginStartupProfile.IsValidFeatureId(featureId)) throw new ArgumentException("Invalid plugin feature ID.",nameof(featureId));
            if (!overrides.TryGetValue(featureId,out var scopes)) { scopes=new List<OverrideScope>(); overrides.Add(featureId,scopes); }
            var scope=new OverrideScope(featureId,enabled,generation); scopes.Add(scope); return scope;
        }
        internal static void ResetSession() { generation++; overrides.Clear(); }
        private sealed class OverrideScope : IDisposable
        {
            internal readonly bool Enabled;
            private readonly string id;
            private readonly int session;
            private bool disposed;
            internal OverrideScope(string id,bool enabled,int session) { this.id=id; Enabled=enabled; this.session=session; }
            public void Dispose()
            {
                if(disposed) return; disposed=true;
                if(session != generation || !overrides.TryGetValue(id,out var scopes)) return;
                scopes.Remove(this); if(scopes.Count==0) overrides.Remove(id);
            }
        }
    }
}
