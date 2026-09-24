using System.Collections.Generic;

namespace StyleTextureConverter
{
    /// <summary>Mask and label helpers shared by region-aware steps. Index = y * width + x; 4-connected.</summary>
    internal static class StyleImageOps
    {
        static readonly int[] Dx = { -1, 1, 0, 0 };
        static readonly int[] Dy = { 0, 0, -1, 1 };

        /// <summary>Chamfer 3/4 distance in texels from each texel to the nearest target texel; 0 on targets.
        /// No target at all returns huge values everywhere.</summary>
        public static float[] DistanceTo(bool[] target, int w, int h)
        {
            const float Far = 1e9f;
            var d = new float[w * h];
            for (int i = 0; i < d.Length; i++)
            {
                d[i] = target[i] ? 0f : Far;
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (d[i] == 0f) continue;
                    float v = d[i];
                    if (x > 0) v = System.Math.Min(v, d[i - 1] + 3f);
                    if (y > 0)
                    {
                        v = System.Math.Min(v, d[i - w] + 3f);
                        if (x > 0) v = System.Math.Min(v, d[i - w - 1] + 4f);
                        if (x < w - 1) v = System.Math.Min(v, d[i - w + 1] + 4f);
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
                    if (x < w - 1) v = System.Math.Min(v, d[i + 1] + 3f);
                    if (y < h - 1)
                    {
                        v = System.Math.Min(v, d[i + w] + 3f);
                        if (x < w - 1) v = System.Math.Min(v, d[i + w + 1] + 4f);
                        if (x > 0) v = System.Math.Min(v, d[i + w - 1] + 4f);
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

        /// <summary>Labels 4-connected runs of equal class. Texels with class below 0 get label -1.</summary>
        public static int[] Components(int[] classes, int w, int h, out int count)
        {
            var labels = new int[classes.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = -1;
            }

            var stack = new Stack<int>();
            count = 0;
            for (int start = 0; start < classes.Length; start++)
            {
                if (classes[start] < 0 || labels[start] >= 0)
                {
                    continue;
                }

                int cls = classes[start];
                labels[start] = count;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    int x = i % w;
                    int y = i / w;
                    for (int n = 0; n < 4; n++)
                    {
                        int nx = x + Dx[n];
                        int ny = y + Dy[n];
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int j = ny * w + nx;
                        if (labels[j] >= 0 || classes[j] != cls) continue;
                        labels[j] = count;
                        stack.Push(j);
                    }
                }
                count++;
            }
            return labels;
        }

        /// <summary>One box-blur pass along rows (horizontal) or columns, radius r, edges clamped.</summary>
        public static float[] BoxBlur(float[] src, int w, int h, int r, bool horizontal)
        {
            var dst = new float[src.Length];
            float norm = 1f / (2 * r + 1);
            int len = horizontal ? w : h;
            int lines = horizontal ? h : w;
            for (int line = 0; line < lines; line++)
            {
                for (int i = 0; i < len; i++)
                {
                    float sum = 0f;
                    for (int k = -r; k <= r; k++)
                    {
                        int j = System.Math.Min(System.Math.Max(i + k, 0), len - 1);
                        sum += horizontal ? src[line * w + j] : src[j * w + line];
                    }
                    dst[horizontal ? line * w + i : i * w + line] = sum * norm;
                }
            }
            return dst;
        }

        public static int[] Classes(bool[] mask)
        {
            var c = new int[mask.Length];
            for (int i = 0; i < c.Length; i++)
            {
                c[i] = mask[i] ? 0 : -1;
            }
            return c;
        }

        /// <summary>Keeps mask texels farther than radius from the nearest non-mask texel.</summary>
        public static bool[] Erode(bool[] mask, int w, int h, float radius)
        {
            if (radius < 1f)
            {
                return (bool[])mask.Clone();
            }

            var outside = new bool[mask.Length];
            for (int i = 0; i < mask.Length; i++)
            {
                outside[i] = !mask[i];
            }

            float[] d = DistanceTo(outside, w, h);
            var result = new bool[mask.Length];
            for (int i = 0; i < mask.Length; i++)
            {
                result[i] = mask[i] && d[i] > radius;
            }
            return result;
        }

        /// <summary>Adds every texel within radius of the mask.</summary>
        public static bool[] Dilate(bool[] mask, int w, int h, float radius)
        {
            if (radius < 1f)
            {
                return (bool[])mask.Clone();
            }

            float[] d = DistanceTo(mask, w, h);
            var result = new bool[mask.Length];
            for (int i = 0; i < mask.Length; i++)
            {
                result[i] = d[i] <= radius;
            }
            return result;
        }
    }
}
