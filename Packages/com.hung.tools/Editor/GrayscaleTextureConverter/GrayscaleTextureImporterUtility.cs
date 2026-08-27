using UnityEditor;
using UnityEngine;

namespace GrayscaleTextureConverter
{
    /// <summary>
    /// Captures source importer settings and configures generated texture importers
    /// to mirror the source where sensible.
    /// </summary>
    public static class GrayscaleTextureImporterUtility
    {
        public class ImporterBackup
        {
            public bool isReadable;
            public TextureImporterType textureType;
            public SpriteImportMode spriteImportMode;
            public float spritePixelsPerUnit;
            public FilterMode filterMode;
            public TextureWrapMode wrapMode;
            public TextureImporterCompression compression;
            public bool mipmapEnabled;
            public TextureImporterAlphaSource alphaSource;
            public bool sRGBTexture;
        }

        /// <summary>
        /// Reads current importer settings without changing or reimporting the source.
        /// Returns null if the asset has no TextureImporter.
        /// </summary>
        public static ImporterBackup Capture(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            var backup = new ImporterBackup
            {
                isReadable = importer.isReadable,
                textureType = importer.textureType,
                spriteImportMode = importer.spriteImportMode,
                spritePixelsPerUnit = importer.spritePixelsPerUnit,
                filterMode = importer.filterMode,
                wrapMode = importer.wrapMode,
                compression = importer.textureCompression,
                mipmapEnabled = importer.mipmapEnabled,
                alphaSource = importer.alphaSource,
                sRGBTexture = importer.sRGBTexture
            };

            return backup;
        }

        /// <summary>
        /// Applies source-mirroring importer settings to a newly generated grayscale texture asset.
        /// Call after AssetDatabase.ImportAsset for the generated file.
        /// </summary>
        public static void ConfigureGeneratedTexture(string generatedAssetPath, ImporterBackup sourceBackup, bool hasAlpha)
        {
            if (sourceBackup == null)
            {
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(generatedAssetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = sourceBackup.textureType;
            importer.spriteImportMode = sourceBackup.spriteImportMode;
            importer.spritePixelsPerUnit = sourceBackup.spritePixelsPerUnit;
            importer.filterMode = sourceBackup.filterMode;
            importer.wrapMode = sourceBackup.wrapMode;
            importer.textureCompression = sourceBackup.compression;
            importer.mipmapEnabled = sourceBackup.mipmapEnabled;
            importer.alphaSource = hasAlpha ? sourceBackup.alphaSource : TextureImporterAlphaSource.None;
            importer.sRGBTexture = sourceBackup.sRGBTexture;
            importer.isReadable = false;

            importer.SaveAndReimport();
        }
    }
}
