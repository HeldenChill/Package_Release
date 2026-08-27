using UnityEngine;

namespace GrayscaleTextureConverter
{
    /// <summary>
    /// Pure pixel math: luminance conversion, adjustments, and optional target-color tint.
    /// No asset or importer knowledge lives here.
    /// </summary>
    public static class GrayscaleTextureProcessor
    {
        /// <summary>
        /// Converts source to a new grayscale Texture2D. Caller owns lifetime of both textures.
        /// Source must be readable (GetPixels32).
        /// </summary>
        public static Texture2D Convert(Texture2D source, GrayscaleTextureConverterSettings settings)
        {
            int width = source.width;
            int height = source.height;

            Color32[] src = source.GetPixels32();
            Color32[] dst = new Color32[src.Length];

            float brightness = settings.brightness;
            float contrast = settings.contrast;
            float gamma = settings.gamma;
            bool invert = settings.invert;
            bool preserveAlpha = settings.preserveAlpha;
            bool colorize = settings.colorize;
            Color targetColor = settings.targetColor;

            for (int i = 0; i < src.Length; i++)
            {
                Color32 c = src[i];

                float r = c.r / 255f;
                float g = c.g / 255f;
                float b = c.b / 255f;

                // 1. RGB -> luminance
                float gray = 0.2126f * r + 0.7152f * g + 0.0722f * b;

                // 2. Gamma
                gray = Mathf.Pow(Mathf.Max(gray, 0f), gamma);

                // 3. Contrast (pivot around mid-gray 0.5)
                gray = (gray - 0.5f) * contrast + 0.5f;

                // 4. Brightness
                gray += brightness;

                // 5. Invert
                if (invert)
                {
                    gray = 1f - gray;
                }

                // 6. Clamp
                gray = Mathf.Clamp01(gray);

                // 7. Alpha
                byte alpha = preserveAlpha ? c.a : (byte)255;

                float outputR = colorize ? gray * targetColor.r : gray;
                float outputG = colorize ? gray * targetColor.g : gray;
                float outputB = colorize ? gray * targetColor.b : gray;

                dst[i] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(outputR) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(outputG) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(outputB) * 255f),
                    alpha);
            }

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            result.SetPixels32(dst);
            result.Apply(false, false);
            return result;
        }
    }
}
