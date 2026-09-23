using System;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Hung.Tool.Editor.StyleTextureConverter.Tests")]

namespace StyleTextureConverter
{
    /// <summary>
    /// Blends a colour into opaque texels within widthTexels of the nearest transparent texel (alpha below 0.5),
    /// so the border hugs the sprite shape. Distance = chamfer 3/4 transform. Widths are export texels (times buf.scale).
    /// </summary>
    [Serializable]
    public sealed class RimStep : StyleStep
    {
        public Color color = Color.white;
        [Min(0f)] public float widthTexels = 10f;
        [Min(0f)] public float softTexels = 2f;
        [Range(0f, 1f)] public float strength = 1f;

        public override string DisplayName => "Rim";

        public override void Apply(StyleBuffer buf)
        {
            float width = widthTexels * buf.scale;
            float soft = Mathf.Min(softTexels * buf.scale, width);
            if (width <= 0f || strength <= 0f)
            {
                return;
            }

            float[] dist = DistanceToTransparent(buf);
            Vector4[] px = buf.pixels;
            for (int i = 0; i < px.Length; i++)
            {
                float d = dist[i];
                if (d <= 0f || d > width)
                {
                    continue;
                }

                float falloff = soft <= 0f || d <= width - soft ? 1f : (width - d) / soft;
                float a = strength * falloff;
                Vector4 p = px[i];
                px[i] = new Vector4(
                    Mathf.Lerp(p.x, color.r, a),
                    Mathf.Lerp(p.y, color.g, a),
                    Mathf.Lerp(p.z, color.b, a),
                    p.w);
            }
        }

        /// <summary>Texel distance from each texel to the nearest transparent texel; 0 on transparent ones.
        /// A texture with no transparent texel returns huge values everywhere.</summary>
        internal static float[] DistanceToTransparent(StyleBuffer buf)
        {
            const float Far = 1e9f;
            int w = buf.width;
            int h = buf.height;
            Vector4[] px = buf.pixels;
            var d = new float[w * h];
            for (int i = 0; i < d.Length; i++)
            {
                d[i] = px[i].w < 0.5f ? 0f : Far;
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (d[i] == 0f) continue;
                    float v = d[i];
                    if (x > 0) v = Mathf.Min(v, d[i - 1] + 3f);
                    if (y > 0)
                    {
                        v = Mathf.Min(v, d[i - w] + 3f);
                        if (x > 0) v = Mathf.Min(v, d[i - w - 1] + 4f);
                        if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + 4f);
                    }
                    d[i] = v;
                }
            }

            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = w - 1; x >= 0; x--)
                {
                    int i = y * w + x;
                    if (d[i] == 0f) continue;
                    float v = d[i];
                    if (x < w - 1) v = Mathf.Min(v, d[i + 1] + 3f);
                    if (y < h - 1)
                    {
                        v = Mathf.Min(v, d[i + w] + 3f);
                        if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + 4f);
                        if (x > 0) v = Mathf.Min(v, d[i + w - 1] + 4f);
                    }
                    d[i] = v;
                }
            }

            for (int i = 0; i < d.Length; i++)
            {
                d[i] /= 3f; // chamfer 3/4 units -> texels
            }
            return d;
        }
    }
}
