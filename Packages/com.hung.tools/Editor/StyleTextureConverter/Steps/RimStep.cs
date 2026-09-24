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
            var transparent = new bool[buf.pixels.Length];
            for (int i = 0; i < transparent.Length; i++)
            {
                transparent[i] = buf.pixels[i].w < 0.5f;
            }
            return StyleImageOps.DistanceTo(transparent, buf.width, buf.height);
        }
    }
}
