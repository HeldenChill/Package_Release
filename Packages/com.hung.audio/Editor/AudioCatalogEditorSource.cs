using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.Base.Editor;
using UnityEditor;

namespace Hung.Audio.Editor
{
    /// <summary>Supplies catalog cue identities to the existing AudioId dropdown infrastructure.</summary>
    [InitializeOnLoad]
    public sealed class AudioCatalogEditorSource : IStableIdEditorSource<AudioId>
    {
        private readonly Func<IEnumerable<AudioCatalog>> scan;
        static AudioCatalogEditorSource() => StableIdEditorRegistry<AudioId>.RegisterSource(new AudioCatalogEditorSource());
        /// <summary>Uses AssetDatabase catalog discovery.</summary>
        public AudioCatalogEditorSource() : this(Scan) { }
        /// <summary>Uses an explicit catalog provider.</summary>
        public AudioCatalogEditorSource(Func<IEnumerable<AudioCatalog>> scan) =>
            this.scan = scan ?? throw new ArgumentNullException(nameof(scan));
        /// <inheritdoc/>
        public IEnumerable<StableIdEditorOption<AudioId>> GetOptions()
        {
            var baseOptions = new Dictionary<AudioId, StableIdEditorOption<AudioId>>();
            foreach (var option in new StableIdConstantsSource<AudioId>(typeof(BaseAudioIds)).GetOptions())
                baseOptions[option.Id] = option;
            foreach (var catalog in scan() ?? Array.Empty<AudioCatalog>())
            {
                if (catalog == null) continue;
                IReadOnlyDictionary<AudioId, AudioCue> index;
                try { index = catalog.BuildIndex(); }
                catch (InvalidOperationException) { continue; }
                foreach (var pair in index)
                    yield return baseOptions.TryGetValue(pair.Key, out var option) ? option :
                        new StableIdEditorOption<AudioId>(pair.Key, pair.Value.CodeName, StableIdGrammar.GetGroup(pair.Key.Value));
            }
        }
        private static IEnumerable<AudioCatalog> Scan()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioCatalog"))
            {
                var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                if (catalog != null) yield return catalog;
            }
        }
    }
}
