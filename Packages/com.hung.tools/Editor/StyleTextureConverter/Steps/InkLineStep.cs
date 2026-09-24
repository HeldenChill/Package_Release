using System;
using UnityEngine;

namespace StyleTextureConverter
{
    /// <summary>
    /// Solid ink on the silhouette (opaque texels near a transparent one) and on borders between posterized
    /// regions (needs a Posterize step earlier; without regions only the outline is drawn). Widths are percent
    /// of the long side. With an outline, the soft alpha fringe (0 < alpha < 0.5) is inked too.
    /// RGB of inked texels = ink; alpha untouched.
    /// </summary>
    [Serializable]
    public sealed class InkLineStep : StyleStep
    {
        public Color ink = new Color(0x27 / 255f, 0x02 / 255f, 0f, 1f);

        [Tooltip("Silhouette line width, % of long side.")]
        [Min(0f)] public float outlinePercent = 1.7f;

        [Tooltip("Line width between posterized regions, % of long side (minimum 2 texels).")]
        [Min(0f)] public float interiorPercent = 0.4f;

        [Tooltip("Fade ink over ~1 texel past its width, so line edges are anti-aliased instead of stair-stepped.")]
        public bool antiAlias;

        public override string DisplayName => "Ink Line";

        public override void Apply(StyleBuffer buf)
        {
            Vector4[] px = buf.pixels;
            var cover = new float[px.Length];

            float outline = buf.PercentToTexels(outlinePercent);
            if (outline > 0f)
            {
                float[] d = RimStep.DistanceToTransparent(buf);
                for (int i = 0; i < px.Length; i++)
                {
                    float a = px[i].w;
                    // soft fringe (0 < alpha < 0.5) takes ink too, so no background RGB bleeds through the AA edge
                    if (a > 0f && a < 0.5f) cover[i] = 1f;
                    else if (a >= 0.5f) cover[i] = Coverage(d[i], outline);
                }
            }

            float interior = buf.PercentToTexels(interiorPercent);
            if (interior > 0f && buf.regions != null && buf.regions.Length == px.Length)
            {
                float[] d = StyleImageOps.DistanceTo(Seams(buf), buf.width, buf.height);
                float reach = Mathf.Max(0f, (interior - 2f) * 0.5f); // a seam is already 2 texels wide
                for (int i = 0; i < px.Length; i++)
                {
                    if (px[i].w >= 0.5f) cover[i] = Mathf.Max(cover[i], Coverage(d[i], reach));
                }
            }

            for (int i = 0; i < px.Length; i++)
            {
                float c = cover[i];
                if (c <= 0f) continue;
                Vector4 p = px[i];
                px[i] = new Vector4(
                    Mathf.Lerp(p.x, ink.r, c),
                    Mathf.Lerp(p.y, ink.g, c),
                    Mathf.Lerp(p.z, ink.b, c),
                    p.w);
            }
        }

        /// <summary>1 within width; with antiAlias, fades to 0 over the next ~1.5 texels.</summary>
        float Coverage(float d, float width)
        {
            if (d <= width) return 1f;
            return antiAlias ? Mathf.Clamp01(width + 1.5f - d) : 0f;
        }

        static bool[] Seams(StyleBuffer buf)
        {
            int w = buf.width;
            int h = buf.height;
            int[] r = buf.regions;
            var seam = new bool[r.Length];
            for (int i = 0; i < r.Length; i++)
            {
                if (r[i] < 0 || buf.pixels[i].w < 0.5f) continue;
                int x = i % w;
                int y = i / w;
                seam[i] = (x > 0 && Differs(r[i - 1]))
                    || (x < w - 1 && Differs(r[i + 1]))
                    || (y > 0 && Differs(r[i - w]))
                    || (y < h - 1 && Differs(r[i + w]));

                bool Differs(int other) => other >= 0 && other != r[i];
            }
            return seam;
        }
    }
}
