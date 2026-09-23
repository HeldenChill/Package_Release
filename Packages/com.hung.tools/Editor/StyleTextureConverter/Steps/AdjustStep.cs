using System;
using UnityEngine;

namespace StyleTextureConverter
{
    /// <summary>Per-channel gamma -> contrast around 0.5 -> brightness (same order as the Grayscale converter),
    /// then saturation around luminance. Values are clamped only at encode.</summary>
    [Serializable]
    public sealed class AdjustStep : StyleStep
    {
        [Range(-1f, 1f)] public float brightness = 0f;
        [Range(0f, 3f)] public float contrast = 1f;
        [Range(0.1f, 3f)] public float gamma = 1f;
        [Range(0f, 3f)] public float saturation = 1f;

        public override string DisplayName => "Adjust";

        public override void Apply(StyleBuffer buf)
        {
            Vector4[] px = buf.pixels;
            for (int i = 0; i < px.Length; i++)
            {
                Vector4 p = px[i];
                float r = Tone(p.x);
                float g = Tone(p.y);
                float b = Tone(p.z);
                float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                px[i] = new Vector4(
                    lum + (r - lum) * saturation,
                    lum + (g - lum) * saturation,
                    lum + (b - lum) * saturation,
                    p.w);
            }
        }

        float Tone(float c)
        {
            c = Mathf.Pow(Mathf.Max(c, 0f), gamma);
            c = (c - 0.5f) * contrast + 0.5f;
            return c + brightness;
        }
    }
}
