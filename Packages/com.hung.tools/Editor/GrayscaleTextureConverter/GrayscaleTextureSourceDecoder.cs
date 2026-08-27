using System;
using System.IO;
using UnityEngine;

namespace GrayscaleTextureConverter
{
    /// <summary>
    /// Decodes source image bytes into an uncompressed, readable scratch texture.
    /// This bypasses imported texture compression and preserves source alpha exactly.
    /// </summary>
    public static class GrayscaleTextureSourceDecoder
    {
        public static bool TryDecode(
            string assetPath,
            int expectedWidth,
            int expectedHeight,
            out Texture2D decoded,
            out string error)
        {
            decoded = null;
            error = null;

            if (string.IsNullOrEmpty(assetPath))
            {
                error = "Source asset path is empty.";
                return false;
            }

            string absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                error = $"Source file does not exist on disk: '{assetPath}'.";
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(absolutePath);
                decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);

                if (!ImageConversion.LoadImage(decoded, bytes, false))
                {
                    DestroyDecoded(ref decoded);
                    error = $"Could not decode source image: '{assetPath}'.";
                    return false;
                }

                if (!ValidateDimensions(decoded.width, decoded.height, expectedWidth, expectedHeight, out error))
                {
                    DestroyDecoded(ref decoded);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                DestroyDecoded(ref decoded);
                error = $"Could not read source image '{assetPath}': {e.Message}";
                return false;
            }
        }

        public static bool ValidateDimensions(
            int actualWidth,
            int actualHeight,
            int expectedWidth,
            int expectedHeight,
            out string error)
        {
            if (actualWidth == expectedWidth && actualHeight == expectedHeight)
            {
                error = null;
                return true;
            }

            error = $"Decoded image dimensions {actualWidth}x{actualHeight} do not match " +
                    $"imported texture dimensions {expectedWidth}x{expectedHeight}.";
            return false;
        }

        static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        static void DestroyDecoded(ref Texture2D decoded)
        {
            if (decoded != null)
            {
                UnityEngine.Object.DestroyImmediate(decoded);
                decoded = null;
            }
        }
    }
}
