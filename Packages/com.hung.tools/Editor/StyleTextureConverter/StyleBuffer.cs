using UnityEngine;

namespace StyleTextureConverter
{
    /// <summary>
    /// Working pixels for one pipeline run. Values are the texture's stored (sRGB) values in 0..1.
    /// </summary>
    public sealed class StyleBuffer
    {
        public readonly int width;
        public readonly int height;

        /// <summary>Output-texel scale: 1 at export resolution, below 1 in the downscaled preview.
        /// Steps with pixel-distance fields multiply by this.</summary>
        public readonly float scale;

        public readonly Vector4[] pixels;

        /// <summary>Per-texel 0..1 key written by a key step; null until one runs.</summary>
        public float[] key;

        public StyleBuffer(int width, int height, float scale)
        {
            this.width = width;
            this.height = height;
            this.scale = scale;
            pixels = new Vector4[width * height];
        }

        public static float Luminance(Vector4 p) => 0.2126f * p.x + 0.7152f * p.y + 0.0722f * p.z;
    }
}
