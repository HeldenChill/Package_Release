using System;
using System.Linq;
using Hung.Base.Composition;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Hung.Audio.Editor
{
    /// <summary>Validates enabled automatic Audio configuration before player builds.</summary>
    public sealed class AudioConfigurationValidator : IPreprocessBuildWithReport
    {
        /// <inheritdoc/>
        public int callbackOrder => 0;
        /// <inheritdoc/>
        public void OnPreprocessBuild(BuildReport report) => Validate();
        /// <summary>Checks the same identity, catalog and capacity rules as runtime startup.</summary>
        public static void Validate()
        {
            if (!PluginStartupPolicy.ShouldStart(AudioPlugin.FeatureId)) return;
            // Use the runtime lookup, including assets below Resources/HungAudio/.
            // The Editor also exposes Resources under Editor folders; players exclude those.
            var matches = Resources.LoadAll<AudioPluginConfiguration>("HungAudio")
                .Select(config => new { Config = config, Path = AssetDatabase.GetAssetPath(config) })
                .Where(match => !match.Path.Replace('\\', '/').Split('/')
                    .Any(part => string.Equals(part, "Editor", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(match => match.Path, StringComparer.Ordinal).ToArray();
            var paths = matches.Select(match => match.Path).ToArray();
            if (matches.Length == 0) throw new BuildFailedException("AUDIO_CONFIG_MISSING: Resources/HungAudio");
            if (matches.Length != 1) throw new BuildFailedException("AUDIO_CONFIG_DUPLICATE: " + string.Join(", ", paths));
            var config = matches[0].Config;
            if (config.Catalog == null) throw new BuildFailedException("AUDIO_CATALOG_MISSING: " + paths[0]);
            if (config.MaxSfxVoices < 1 || config.MaxSfxVoices > 128) throw new BuildFailedException("AUDIO_VOICE_CAP_INVALID: " + paths[0]);
            try { config.Catalog.BuildIndex(); }
            catch (Exception error) { throw new BuildFailedException("AUDIO_CATALOG_INVALID: " + AssetDatabase.GetAssetPath(config.Catalog) + ": " + error.Message); }
        }
    }
}
