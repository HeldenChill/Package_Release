using System;
using System.Collections.Generic;
using Hung.Base.Editor;
using UnityEditor;

namespace Hung.Audio.Editor
{
    /// <summary>Generates product AudioId constants through shared validation and deterministic emission.</summary>
    public static class AudioCatalogCodeGenerator
    {
        /// <summary>Returns source for GameAudioIds, excluding existing base-prefixed IDs.</summary>
        public static string GenerateContent(AudioCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var rows = new List<KeyValuePair<string, string>>();
            foreach (var pair in catalog.BuildIndex()) rows.Add(new KeyValuePair<string, string>(pair.Value.CodeName, pair.Key.Value));
            return StableIdCodeGenerator.GenerateContent("AudioId", "Hung.Base", "GameAudioIds", rows, "base.");
        }
        [MenuItem("Assets/Hung Audio/Generate GameAudioIds", true)]
        private static bool CanGenerate() => Selection.activeObject is AudioCatalog;
        [MenuItem("Assets/Hung Audio/Generate GameAudioIds")]
        private static void Generate()
        {
            var catalog = Selection.activeObject as AudioCatalog;
            if (catalog == null) return;
            string content = GenerateContent(catalog);
            string path = EditorUtility.SaveFilePanelInProject("Generate Audio IDs", "GameAudioIds", "cs", "Select product source location.");
            if (string.IsNullOrEmpty(path)) return;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Generate into product Assets only.");
            StableIdCodeGenerator.WriteIfChanged(path, content);
        }
    }
}
