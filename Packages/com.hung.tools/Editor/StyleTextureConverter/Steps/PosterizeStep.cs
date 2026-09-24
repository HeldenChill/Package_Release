using System;
using System.Collections.Generic;
using UnityEngine;

namespace StyleTextureConverter
{
    /// <summary>
    /// Flat cel colours: k-means over opaque texels, every opaque texel snapped to its centroid, colour patches
    /// smaller than (minRegionPercent of long side)^2 texels merged into their most common neighbour.
    /// Writes StyleBuffer.regions (centroid index, -1 on non-opaque texels). Alpha untouched.
    /// </summary>
    [Serializable]
    public sealed class PosterizeStep : StyleStep
    {
        [Range(2, 32)] public int colors = 6;
        [Min(1)] public int maxSamples = 65536;

        [Tooltip("Patches smaller than (this % of long side)^2 texels merge into their most common neighbour.")]
        [Min(0f)] public float minRegionPercent = 1.5f;

        [Tooltip("Blur radius (% of long side) applied to the colours used for clustering and assignment, so region "
            + "borders follow smooth contours instead of pixel noise. Output colours are still flat centroids. 0 = off.")]
        [Min(0f)] public float smoothPercent = 0f;

        public int seed = 1;

        public override string DisplayName => "Posterize";

        public override void Apply(StyleBuffer buf)
        {
            Vector4[] px = buf.pixels;
            var opaque = new List<int>();
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].w >= 0.5f) opaque.Add(i);
            }
            if (opaque.Count == 0)
            {
                return;
            }

            Vector3[] colour = Smoothed(buf);
            int k = Mathf.Min(Mathf.Max(colors, 2), opaque.Count);
            int budget = Mathf.Max(1, maxSamples);
            int stride = (opaque.Count + budget - 1) / budget;
            var samples = new List<Vector3>(opaque.Count / stride + 1);
            for (int s = 0; s < opaque.Count; s += stride)
            {
                samples.Add(colour[opaque[s]]);
            }

            Vector3[] centroids = KMeans(samples, k, seed);

            var labels = new int[px.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = -1;
            }
            foreach (int i in opaque)
            {
                labels[i] = Nearest(centroids, colour[i]);
            }

            float side = buf.PercentToTexels(minRegionPercent);
            MergeSmall(labels, buf.width, buf.height, Mathf.RoundToInt(side * side), centroids.Length);

            foreach (int i in opaque)
            {
                Vector3 c = centroids[labels[i]];
                px[i] = new Vector4(c.x, c.y, c.z, px[i].w);
            }
            buf.regions = labels;
        }

        /// <summary>k-means++ init from seed, at most 20 Lloyd iterations, stops when no centroid moves 1/1024.
        /// Duplicate samples are fine: an init pick with zero total distance reuses centroid 0.</summary>
        internal static Vector3[] KMeans(List<Vector3> samples, int k, int seed)
        {
            var rng = new System.Random(seed);
            var c = new Vector3[k];
            c[0] = samples[rng.Next(samples.Count)];
            var d2 = new float[samples.Count];
            for (int j = 1; j < k; j++)
            {
                double total = 0;
                for (int s = 0; s < samples.Count; s++)
                {
                    float best = float.MaxValue;
                    for (int m = 0; m < j; m++)
                    {
                        best = Mathf.Min(best, (samples[s] - c[m]).sqrMagnitude);
                    }
                    d2[s] = best;
                    total += best;
                }

                if (total <= 0)
                {
                    c[j] = c[0];
                    continue;
                }

                double pick = rng.NextDouble() * total;
                int chosen = samples.Count - 1;
                for (int s = 0; s < samples.Count; s++)
                {
                    pick -= d2[s];
                    if (pick <= 0)
                    {
                        chosen = s;
                        break;
                    }
                }
                c[j] = samples[chosen];
            }

            var sums = new Vector3[k];
            var counts = new int[k];
            for (int iter = 0; iter < 20; iter++)
            {
                Array.Clear(sums, 0, k);
                Array.Clear(counts, 0, k);
                foreach (Vector3 s in samples)
                {
                    int a = Nearest(c, s);
                    sums[a] += s;
                    counts[a]++;
                }

                float move = 0f;
                for (int j = 0; j < k; j++)
                {
                    if (counts[j] == 0) continue;
                    Vector3 m = sums[j] / counts[j];
                    move = Mathf.Max(move, (m - c[j]).magnitude);
                    c[j] = m;
                }
                if (move < 1f / 1024f) break;
            }
            return c;
        }

        // ponytail: up to 4 passes so specks merged into other specks settle; raise if speckle survives on real art
        static void MergeSmall(int[] labels, int w, int h, int minArea, int labelCount)
        {
            if (minArea <= 1)
            {
                return;
            }

            for (int pass = 0; pass < 4; pass++)
            {
                int[] comp = StyleImageOps.Components(labels, w, h, out int count);
                var size = new int[count];
                foreach (int c in comp)
                {
                    if (c >= 0) size[c]++;
                }

                var votes = new Dictionary<int, int[]>();
                for (int i = 0; i < labels.Length; i++)
                {
                    int c = comp[i];
                    if (c < 0 || size[c] >= minArea) continue;
                    int x = i % w;
                    int y = i / w;
                    if (x > 0) Vote(c, i - 1);
                    if (x < w - 1) Vote(c, i + 1);
                    if (y > 0) Vote(c, i - w);
                    if (y < h - 1) Vote(c, i + w);
                }
                if (votes.Count == 0)
                {
                    return;
                }

                var target = new Dictionary<int, int>();
                foreach (KeyValuePair<int, int[]> v in votes)
                {
                    int best = -1;
                    for (int l = 0; l < labelCount; l++)
                    {
                        if (v.Value[l] > 0 && (best < 0 || v.Value[l] > v.Value[best])) best = l;
                    }
                    if (best >= 0) target[v.Key] = best;
                }

                bool changed = false;
                for (int i = 0; i < labels.Length; i++)
                {
                    int c = comp[i];
                    if (c >= 0 && target.TryGetValue(c, out int l))
                    {
                        labels[i] = l;
                        changed = true;
                    }
                }
                if (!changed)
                {
                    return;
                }

                void Vote(int c, int j)
                {
                    if (labels[j] < 0 || comp[j] == c) return;
                    if (!votes.TryGetValue(c, out int[] tally))
                    {
                        tally = new int[labelCount];
                        votes[c] = tally;
                    }
                    tally[labels[j]]++;
                }
            }
        }

        /// <summary>Per-texel RGB, tent-blurred (two box passes) when smoothPercent is set.</summary>
        Vector3[] Smoothed(StyleBuffer buf)
        {
            Vector4[] px = buf.pixels;
            var rgb = new Vector3[px.Length];
            int r = Mathf.RoundToInt(buf.PercentToTexels(smoothPercent) * 0.5f);
            if (r < 1)
            {
                for (int i = 0; i < px.Length; i++) rgb[i] = Rgb(px[i]);
                return rgb;
            }

            int w = buf.width;
            int h = buf.height;
            for (int c = 0; c < 3; c++)
            {
                var ch = new float[px.Length];
                for (int i = 0; i < px.Length; i++) ch[i] = px[i][c];
                for (int pass = 0; pass < 2; pass++)
                {
                    ch = StyleImageOps.BoxBlur(ch, w, h, r, true);
                    ch = StyleImageOps.BoxBlur(ch, w, h, r, false);
                }
                for (int i = 0; i < px.Length; i++) rgb[i][c] = ch[i];
            }
            return rgb;
        }

        static int Nearest(Vector3[] c, Vector3 p)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int j = 0; j < c.Length; j++)
            {
                float d = (p - c[j]).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = j;
                }
            }
            return best;
        }

        static Vector3 Rgb(Vector4 p) => new Vector3(p.x, p.y, p.z);
    }
}
