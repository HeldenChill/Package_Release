using System.IO;
using GrayscaleTextureConverter;
using UnityEditor;
using UnityEngine;

namespace StyleTextureConverter
{
    public static class StyleTextureExport
    {
        static readonly string[] Platforms = { "Standalone", "Android", "iPhone", "WebGL" };

        public static string OutputPath(string sourceAssetPath, string suffix)
        {
            string folder = Path.GetDirectoryName(sourceAssetPath)?.Replace('\\', '/');
            return $"{folder}/{Path.GetFileNameWithoutExtension(sourceAssetPath)}{suffix}.png";
        }

        /// <summary>
        /// Decodes the source file at full size. Validates against the importer's SOURCE size, not the imported
        /// Texture2D size, which is capped by maxTextureSize for the active build target.
        /// </summary>
        public static bool TryDecodeSource(string assetPath, out Texture2D decoded, out string error)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
            {
                decoded = null;
                error = $"'{assetPath}' has no TextureImporter.";
                return false;
            }

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            return GrayscaleTextureSourceDecoder.TryDecode(assetPath, width, height, out decoded, out error);
        }

        /// <summary>
        /// Writes result as PNG beside the source, imports it, and mirrors the source importer
        /// (base settings via the Grayscale importer utility + per-platform overrides).
        /// Overwrites an existing output silently — confirm in the caller.
        /// </summary>
        public static string Export(Texture2D result, string sourceAssetPath, string suffix, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(suffix))
            {
                error = "Output suffix is empty; refusing to overwrite the source.";
                return null;
            }

            string outPath = OutputPath(sourceAssetPath, suffix);
            GrayscaleTextureImporterUtility.ImporterBackup backup = GrayscaleTextureImporterUtility.Capture(sourceAssetPath);

            try
            {
                File.WriteAllBytes(outPath, result.EncodeToPNG());
            }
            catch (System.Exception e)
            {
                error = $"Failed writing '{outPath}': {e.Message}";
                return null;
            }

            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
            GrayscaleTextureImporterUtility.ConfigureGeneratedTexture(outPath, backup, true);
            MirrorImporter(sourceAssetPath, outPath);
            return outPath;
        }

        /// <summary>
        /// Mirrors what the Grayscale importer utility does not: base texture settings (npot, alphaIsTransparency,
        /// sprite mode...), base maxTextureSize, and per-platform overrides.
        /// ponytail: sprite slice rects are NOT copied (needs ISpriteEditorDataProvider); re-slice Multiple sheets by hand.
        /// </summary>
        static void MirrorImporter(string sourcePath, string outPath)
        {
            var source = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            var output = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (source == null || output == null)
            {
                return;
            }

            var settings = new TextureImporterSettings();
            source.ReadTextureSettings(settings);
            output.SetTextureSettings(settings);
            output.maxTextureSize = source.maxTextureSize;

            foreach (string platform in Platforms)
            {
                TextureImporterPlatformSettings platformSettings = source.GetPlatformTextureSettings(platform);
                if (platformSettings.overridden)
                {
                    output.SetPlatformTextureSettings(platformSettings);
                }
            }

            output.SaveAndReimport();
        }
    }
}
