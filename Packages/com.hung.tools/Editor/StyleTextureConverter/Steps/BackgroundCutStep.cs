using System;
using System.Collections.Generic;
using UnityEngine;

namespace StyleTextureConverter
{
    /// <summary>
    /// Cuts an opaque background: floods low-gradient texels from the image border, then open -> largest
    /// component -> fill holes -> shrink. The flood runs on a copy box-downscaled to workingSize (soft glows read
    /// as edges there, and the cut does not depend on source resolution); its mask is upscaled bilinearly. Writes alpha 1 on the subject, 0 elsewhere, and never makes a transparent
    /// source texel opaque. RGB untouched. The only step that changes alpha, so it clears StyleBuffer.regions.
    /// Distances are percent of the long side.
    /// </summary>
    [Serializable]
    public sealed class BackgroundCutStep : StyleStep
    {
        [Tooltip("Colour-edge strength that stops the background flood. 1 = hard black/white step.")]
        [Range(0f, 1f)] public float gradientThreshold = 0.35f;

        [Tooltip("Open radius, % of long side. Detaches thin specks glued to the subject.")]
        [Min(0f)] public float openPercent = 0.4f;

        [Tooltip("Final erode, % of long side. Eats a sticker stroke around the subject.")]
        [Min(0f)] public float shrinkPercent = 0.8f;

        [Tooltip("Long side of the box-downscaled copy the background flood runs on. Sources at or below it run at full size.")]
        [Min(16)] public int workingSize = 256;

        [Tooltip("Soft alpha edge width, % of long side. 0 = hard 0/1 edge.")]
        [Min(0f)] public float featherPercent = 0.2f;

        public bool keepLargestOnly = true;
        public bool fillHoles = true;

        public override string DisplayName => "Background Cut";

        public override void Apply(StyleBuffer buf)
        {
            int w = buf.width;
            int h = buf.height;
            bool[] background = BackgroundMask(buf);
            if (background == null)
            {
                Warn("no background reached from border; raise gradientThreshold");
                return;
            }

            var fg = new bool[background.Length];
            for (int i = 0; i < fg.Length; i++)
            {
                fg[i] = !background[i];
            }
            if (!Any(fg))
            {
                Warn("whole image reached from border; lower gradientThreshold");
                return;
            }

            float open = buf.PercentToTexels(openPercent);
            fg = StyleImageOps.Dilate(StyleImageOps.Erode(fg, w, h, open), w, h, open);
            if (!Any(fg))
            {
                Warn("nothing left after open; lower openPercent");
                return;
            }

            if (keepLargestOnly)
            {
                fg = KeepLargest(fg, w, h);
            }
            if (fillHoles)
            {
                fg = FillHoles(fg, w, h);
            }

            fg = StyleImageOps.Erode(fg, w, h, buf.PercentToTexels(shrinkPercent));
            if (!Any(fg))
            {
                Warn("nothing left after shrink; lower shrinkPercent");
                return;
            }

            float[] alpha = Feather(fg, w, h, buf.PercentToTexels(featherPercent));
            Vector4[] px = buf.pixels;
            for (int i = 0; i < px.Length; i++)
            {
                Vector4 p = px[i];
                px[i] = new Vector4(p.x, p.y, p.z, p.w >= 0.5f ? alpha[i] : 0f);
            }
            buf.regions = null;
        }

        /// <summary>Sobel magnitude per RGB channel, max over channels, divided by 4 (hard 0-to-1 step = 1).
        /// Samples outside the image clamp to the edge.</summary>
        internal static float[] Gradient(StyleBuffer buf)
        {
            int w = buf.width;
            int h = buf.height;
            Vector4[] px = buf.pixels;
            var g = new float[w * h];
            for (int y = 0; y < h; y++)
            {
                int y0 = Mathf.Max(y - 1, 0);
                int y1 = Mathf.Min(y + 1, h - 1);
                for (int x = 0; x < w; x++)
                {
                    int x0 = Mathf.Max(x - 1, 0);
                    int x1 = Mathf.Min(x + 1, w - 1);
                    float best = 0f;
                    for (int c = 0; c < 3; c++)
                    {
                        float tl = px[y0 * w + x0][c], tc = px[y0 * w + x][c], tr = px[y0 * w + x1][c];
                        float ml = px[y * w + x0][c], mr = px[y * w + x1][c];
                        float bl = px[y1 * w + x0][c], bc = px[y1 * w + x][c], br = px[y1 * w + x1][c];
                        float gx = (tr + 2f * mr + br) - (tl + 2f * ml + bl);
                        float gy = (bl + 2f * bc + br) - (tl + 2f * tc + tr);
                        best = Mathf.Max(best, Mathf.Sqrt(gx * gx + gy * gy) * 0.25f);
                    }
                    g[y * w + x] = best;
                }
            }
            return g;
        }

        /// <summary>Full-size background mask decided at workingSize, or null when no border texel is passable.</summary>
        bool[] BackgroundMask(StyleBuffer buf)
        {
            int w = buf.width;
            int h = buf.height;
            int longSide = Mathf.Max(w, h);
            if (longSide <= workingSize)
            {
                return FloodFromBorder(buf, gradientThreshold);
            }

            float f = (float)workingSize / longSide;
            int sw = Mathf.Max(1, Mathf.RoundToInt(w * f));
            int sh = Mathf.Max(1, Mathf.RoundToInt(h * f));
            bool[] small = FloodFromBorder(BoxDownscale(buf, sw, sh), gradientThreshold);
            if (small == null)
            {
                return null;
            }

            // bilinear upscale of the 0/1 mask, cut at 0.5: smooth diagonals instead of workingSize staircases
            var bg = new bool[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = Mathf.Clamp((y + 0.5f) * sh / h - 0.5f, 0f, sh - 1);
                int y0 = (int)v;
                int y1 = Mathf.Min(y0 + 1, sh - 1);
                float ty = v - y0;
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Clamp((x + 0.5f) * sw / w - 0.5f, 0f, sw - 1);
                    int x0 = (int)u;
                    int x1 = Mathf.Min(x0 + 1, sw - 1);
                    float tx = u - x0;
                    float top = Mathf.Lerp(small[y0 * sw + x0] ? 1f : 0f, small[y0 * sw + x1] ? 1f : 0f, tx);
                    float bottom = Mathf.Lerp(small[y1 * sw + x0] ? 1f : 0f, small[y1 * sw + x1] ? 1f : 0f, tx);
                    bg[y * w + x] = Mathf.Lerp(top, bottom, ty) >= 0.5f;
                }
            }
            return bg;
        }

        static StyleBuffer BoxDownscale(StyleBuffer buf, int sw, int sh)
        {
            int w = buf.width;
            int h = buf.height;
            var small = new StyleBuffer(sw, sh, buf.scale * sw / w);
            for (int y = 0; y < sh; y++)
            {
                int ya = y * h / sh;
                int yb = Mathf.Max(ya + 1, (y + 1) * h / sh);
                for (int x = 0; x < sw; x++)
                {
                    int xa = x * w / sw;
                    int xb = Mathf.Max(xa + 1, (x + 1) * w / sw);
                    Vector4 sum = Vector4.zero;
                    for (int yy = ya; yy < yb; yy++)
                    {
                        for (int xx = xa; xx < xb; xx++)
                        {
                            sum += buf.pixels[yy * w + xx];
                        }
                    }
                    small.pixels[y * sw + x] = sum / ((yb - ya) * (xb - xa));
                }
            }
            return small;
        }

        /// <summary>Background mask, or null when no border texel is passable.</summary>
        static bool[] FloodFromBorder(StyleBuffer buf, float threshold)
        {
            int w = buf.width;
            int h = buf.height;
            Vector4[] px = buf.pixels;
            float[] grad = Gradient(buf);
            var passable = new bool[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                passable[i] = px[i].w < 0.5f || grad[i] < threshold;
            }

            var reached = new bool[px.Length];
            var stack = new Stack<int>();
            for (int x = 0; x < w; x++)
            {
                Seed(x);
                Seed((h - 1) * w + x);
            }
            for (int y = 0; y < h; y++)
            {
                Seed(y * w);
                Seed(y * w + w - 1);
            }
            if (stack.Count == 0)
            {
                return null;
            }

            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int x = i % w;
                int y = i / w;
                if (x > 0) Seed(i - 1);
                if (x < w - 1) Seed(i + 1);
                if (y > 0) Seed(i - w);
                if (y < h - 1) Seed(i + w);
            }
            return reached;

            void Seed(int j)
            {
                if (reached[j] || !passable[j]) return;
                reached[j] = true;
                stack.Push(j);
            }
        }

        static bool[] KeepLargest(bool[] fg, int w, int h)
        {
            int[] labels = StyleImageOps.Components(StyleImageOps.Classes(fg), w, h, out int count);
            var size = new int[count];
            foreach (int l in labels)
            {
                if (l >= 0) size[l]++;
            }

            int best = 0;
            for (int l = 1; l < count; l++)
            {
                if (size[l] > size[best]) best = l;
            }

            var result = new bool[fg.Length];
            for (int i = 0; i < fg.Length; i++)
            {
                result[i] = labels[i] == best;
            }
            return result;
        }

        /// <summary>Non-subject pockets not connected to the image border become subject.</summary>
        internal static bool[] FillHoles(bool[] fg, int w, int h)
        {
            var outside = new bool[fg.Length];
            for (int i = 0; i < fg.Length; i++)
            {
                outside[i] = !fg[i];
            }

            int[] labels = StyleImageOps.Components(StyleImageOps.Classes(outside), w, h, out int count);
            var touchesBorder = new bool[count];
            for (int x = 0; x < w; x++)
            {
                Mark(labels[x]);
                Mark(labels[(h - 1) * w + x]);
            }
            for (int y = 0; y < h; y++)
            {
                Mark(labels[y * w]);
                Mark(labels[y * w + w - 1]);
            }

            var result = (bool[])fg.Clone();
            for (int i = 0; i < fg.Length; i++)
            {
                if (labels[i] >= 0 && !touchesBorder[labels[i]])
                {
                    result[i] = true;
                }
            }
            return result;

            void Mark(int l)
            {
                if (l >= 0) touchesBorder[l] = true;
            }
        }

        /// <summary>0/1 mask blurred with a tent filter (two box passes) so the silhouette is anti-aliased.
        /// Symmetric, so the 0.5 crossing stays on a straight edge's original position.</summary>
        static float[] Feather(bool[] fg, int w, int h, float width)
        {
            var a = new float[fg.Length];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = fg[i] ? 1f : 0f;
            }

            int r = Mathf.RoundToInt(width * 0.5f);
            if (r < 1)
            {
                return a;
            }

            // ponytail: tent filter hides mask staircases; gaussian if edges still read jagged
            for (int pass = 0; pass < 2; pass++)
            {
                a = StyleImageOps.BoxBlur(a, w, h, r, true);
                a = StyleImageOps.BoxBlur(a, w, h, r, false);
            }
            return a;
        }

        static bool Any(bool[] m)
        {
            foreach (bool b in m)
            {
                if (b) return true;
            }
            return false;
        }

        static void Warn(string message) => Debug.LogWarning("[StyleTextureConverter] BackgroundCut: " + message);
    }
}
