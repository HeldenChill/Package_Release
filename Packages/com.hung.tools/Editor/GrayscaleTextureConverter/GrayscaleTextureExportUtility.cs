using System.IO;
using UnityEditor;
using UnityEngine;

namespace GrayscaleTextureConverter
{
    public static class GrayscaleTextureExportUtility
    {
        /// <summary>
        /// Encodes a texture and writes it to disk under the given settings and source path.
        /// Returns the final asset-relative output path, or null if skipped/failed.
        /// </summary>
        public static string Export(Texture2D grayscale, bool sourceHasAlpha, string sourceAssetPath, GrayscaleTextureConverterSettings settings, out string error)
        {
            error = null;

            string sourceFolder = Path.GetDirectoryName(sourceAssetPath)?.Replace('\\', '/');
            string sourceName = Path.GetFileNameWithoutExtension(sourceAssetPath);

            string targetFolder = settings.outputLocation == OutputLocation.CustomFolder
                ? settings.customOutputFolder.TrimEnd('/')
                : sourceFolder;

            if (string.IsNullOrEmpty(targetFolder))
            {
                error = $"Invalid output folder for '{sourceAssetPath}'.";
                return null;
            }

            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                error = $"Output folder '{targetFolder}' does not exist.";
                return null;
            }

            // PNG is forced when the source has transparency, per spec.
            OutputFormat format = sourceHasAlpha ? OutputFormat.PNG : settings.outputFormat;
            string extension = format == OutputFormat.PNG ? "png" : "jpg";

            string baseFileName = sourceName + settings.filenameSuffix;
            string candidatePath = $"{targetFolder}/{baseFileName}.{extension}";

            if (File.Exists(ToAbsolutePath(candidatePath)))
            {
                switch (settings.existingFileBehavior)
                {
                    case ExistingFileBehavior.Skip:
                        return null;
                    case ExistingFileBehavior.UniqueFilename:
                        candidatePath = GenerateUniquePath(targetFolder, baseFileName, extension);
                        break;
                    case ExistingFileBehavior.Replace:
                        break;
                }
            }

            byte[] bytes = format == OutputFormat.PNG
                ? grayscale.EncodeToPNG()
                : grayscale.EncodeToJPG(settings.jpgQuality);

            if (bytes == null || bytes.Length == 0)
            {
                error = $"Encoding failed for '{sourceAssetPath}' (format {format}).";
                return null;
            }

            string absolutePath = ToAbsolutePath(candidatePath);

            try
            {
                File.WriteAllBytes(absolutePath, bytes);
            }
            catch (System.Exception e)
            {
                error = $"Failed writing '{candidatePath}': {e.Message}";
                return null;
            }

            return candidatePath;
        }

        static string GenerateUniquePath(string folder, string baseFileName, string extension)
        {
            string path = $"{folder}/{baseFileName}.{extension}";
            int counter = 1;
            while (File.Exists(ToAbsolutePath(path)))
            {
                path = $"{folder}/{baseFileName}_{counter}.{extension}";
                counter++;
            }
            return path;
        }

        static string ToAbsolutePath(string assetRelativePath)
        {
            // assetRelativePath is like "Assets/Foo/Bar.png"
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetRelativePath).Replace('\\', '/');
        }
    }
}
