using System;
using System.Collections.Generic;
using UnityEngine;

namespace StyleTextureConverter
{
    public sealed class StyleStepException : Exception
    {
        public readonly int index;

        public StyleStepException(int index, string stepName, Exception inner)
            : base($"Step {index} {stepName}: {inner.Message}", inner)
        {
            this.index = index;
        }
    }

    /// <summary>
    /// Pure pixel pipeline: texture -> StyleBuffer -> enabled steps in order -> texture. No asset or IO code.
    /// </summary>
    public static class StyleTextureProcessor
    {
        /// <summary>Source must be readable. Caller owns both textures.</summary>
        public static Texture2D Run(Texture2D source, StyleProfile profile, float scale = 1f)
        {
            StyleBuffer buf = ToBuffer(source, scale);
            RunSteps(buf, profile.steps);
            return ToTexture(buf);
        }

        public static void RunSteps(StyleBuffer buf, IReadOnlyList<StyleStep> steps)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                StyleStep step = steps[i];
                if (step == null)
                {
                    Debug.LogWarning($"[StyleTextureConverter] Step {i} is missing (class deleted or renamed); skipped.");
                    continue;
                }

                if (!step.enabled)
                {
                    continue;
                }

                try
                {
                    step.Apply(buf);
                }
                catch (Exception e)
                {
                    throw new StyleStepException(i, step.DisplayName, e);
                }
            }
        }

        public static StyleBuffer ToBuffer(Texture2D source, float scale)
        {
            Color32[] src = source.GetPixels32();
            var buf = new StyleBuffer(source.width, source.height, scale);
            for (int i = 0; i < src.Length; i++)
            {
                Color32 c = src[i];
                buf.pixels[i] = new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
            }
            return buf;
        }

        public static Texture2D ToTexture(StyleBuffer buf)
        {
            var dst = new Color32[buf.pixels.Length];
            for (int i = 0; i < dst.Length; i++)
            {
                Vector4 p = buf.pixels[i];
                dst[i] = new Color32(ToByte(p.x), ToByte(p.y), ToByte(p.z), ToByte(p.w));
            }

            var result = new Texture2D(buf.width, buf.height, TextureFormat.RGBA32, false, false);
            result.SetPixels32(dst);
            result.Apply(false, false);
            return result;
        }

        /// <summary>
        /// Returns a NEW readable texture whose long side is at most maxSide (never upscaled).
        /// scale = result width / source width.
        /// </summary>
        public static Texture2D Downscale(Texture2D source, int maxSide, out float scale)
        {
            int longSide = Mathf.Max(source.width, source.height);
            float ratio = longSide <= maxSide ? 1f : (float)maxSide / longSide;
            int w = Mathf.Max(1, Mathf.RoundToInt(source.width * ratio));
            int h = Mathf.Max(1, Mathf.RoundToInt(source.height * ratio));
            scale = (float)w / source.width;

            Color32[] src = source.GetPixels32();
            var dst = new Color32[w * h];
            // ponytail: nearest sampling at texel centres, preview only; box filter if preview aliasing matters
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Min(source.height - 1, (int)((y + 0.5f) * source.height / h));
                for (int x = 0; x < w; x++)
                {
                    int sx = Mathf.Min(source.width - 1, (int)((x + 0.5f) * source.width / w));
                    dst[y * w + x] = src[sy * source.width + sx];
                }
            }

            var result = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            result.SetPixels32(dst);
            result.Apply(false, false);
            return result;
        }

        static byte ToByte(float v) => (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
    }
}
