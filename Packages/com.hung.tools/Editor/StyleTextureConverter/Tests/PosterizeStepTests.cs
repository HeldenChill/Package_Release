using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class PosterizeStepTests
    {
        /// Left half red, right half blue, each with +-0.05 deterministic noise.
        static StyleBuffer NoisyTwoTone(int w, int h)
        {
            var buf = new StyleBuffer(w, h, 1f);
            var rng = new System.Random(7);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float n = (float)(rng.NextDouble() * 0.1 - 0.05);
                    buf.pixels[y * w + x] = x < w / 2
                        ? new Vector4(0.8f + n, 0.1f + n, 0.1f + n, 1f)
                        : new Vector4(0.1f + n, 0.1f + n, 0.8f + n, 1f);
                }
            }
            return buf;
        }

        [Test]
        public void TwoToneNoiseCollapsesToTwoColours()
        {
            StyleBuffer buf = NoisyTwoTone(16, 8);
            new PosterizeStep { colors = 2, minRegionPercent = 0f }.Apply(buf);
            var distinct = new HashSet<Vector4>(buf.pixels);
            Assert.AreEqual(2, distinct.Count);
            Assert.AreEqual(0.8f, buf.pixels[0].x, 0.03f);
            Assert.AreEqual(0.8f, buf.pixels[15].z, 0.03f);
        }

        [Test]
        public void RegionsHoldLabelPerTexel()
        {
            StyleBuffer buf = NoisyTwoTone(16, 8);
            new PosterizeStep { colors = 2, minRegionPercent = 0f }.Apply(buf);
            Assert.IsNotNull(buf.regions);
            Assert.AreNotEqual(buf.regions[0], buf.regions[15]);
            Assert.AreEqual(buf.regions[0], buf.regions[7]);
        }

        [Test]
        public void SmallRegionMergesIntoNeighbour()
        {
            // 20x20 red with one blue texel; min area (10% of 20 = 2 texels)^2 = 4 > 1
            var buf = new StyleBuffer(20, 20, 1f);
            for (int i = 0; i < buf.pixels.Length; i++) buf.pixels[i] = new Vector4(0.8f, 0.1f, 0.1f, 1f);
            buf.pixels[10 * 20 + 10] = new Vector4(0.1f, 0.1f, 0.8f, 1f);
            new PosterizeStep { colors = 2, minRegionPercent = 10f }.Apply(buf);
            Assert.AreEqual(buf.regions[0], buf.regions[10 * 20 + 10]);
            Assert.AreEqual(buf.pixels[0], buf.pixels[10 * 20 + 10]);
        }

        [Test]
        public void TransparentTexelsGetRegionMinusOneAndKeepRgb()
        {
            StyleBuffer buf = NoisyTwoTone(16, 8);
            buf.pixels[0] = new Vector4(0.3f, 0.3f, 0.3f, 0.4f); // semi-transparent fringe
            new PosterizeStep { colors = 2, minRegionPercent = 0f }.Apply(buf);
            Assert.AreEqual(-1, buf.regions[0]);
            Assert.AreEqual(new Vector4(0.3f, 0.3f, 0.3f, 0.4f), buf.pixels[0]);
        }

        [Test]
        public void DeterministicForFixedSeed()
        {
            StyleBuffer a = NoisyTwoTone(16, 8);
            StyleBuffer b = NoisyTwoTone(16, 8);
            new PosterizeStep { colors = 4, minRegionPercent = 0f, seed = 3 }.Apply(a);
            new PosterizeStep { colors = 4, minRegionPercent = 0f, seed = 3 }.Apply(b);
            CollectionAssert.AreEqual(a.pixels, b.pixels);
        }

        [Test]
        public void FewerDistinctColoursThanKStaysFinite()
        {
            var buf = new StyleBuffer(4, 1, 1f);
            buf.pixels[0] = buf.pixels[1] = new Vector4(1f, 0f, 0f, 1f);
            buf.pixels[2] = buf.pixels[3] = new Vector4(0f, 0f, 1f, 1f);
            new PosterizeStep { colors = 6, minRegionPercent = 0f }.Apply(buf);
            foreach (Vector4 p in buf.pixels)
            {
                Assert.IsFalse(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z));
            }
            Assert.AreEqual(new Vector4(1f, 0f, 0f, 1f), buf.pixels[0]);
            Assert.AreEqual(new Vector4(0f, 0f, 1f, 1f), buf.pixels[3]);
        }

        [Test]
        public void NoOpaqueTexelsIsNoOp()
        {
            var buf = new StyleBuffer(2, 1, 1f);
            buf.pixels[0] = buf.pixels[1] = new Vector4(0.5f, 0.5f, 0.5f, 0f);
            new PosterizeStep().Apply(buf);
            Assert.IsNull(buf.regions);
            Assert.AreEqual(new Vector4(0.5f, 0.5f, 0.5f, 0f), buf.pixels[0]);
        }

        /// 16x16: red for x <= 7, blue for x >= 8, plus a 2-texel red bump at (8..9, 5).
        static StyleBuffer BumpedEdge()
        {
            var buf = new StyleBuffer(16, 16, 1f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool red = x <= 7 || (y == 5 && x <= 9);
                    buf.pixels[y * 16 + x] = red ? new Vector4(0.9f, 0.1f, 0.1f, 1f) : new Vector4(0.1f, 0.1f, 0.9f, 1f);
                }
            }
            return buf;
        }

        [Test]
        public void SmoothingRemovesBorderBump()
        {
            StyleBuffer raw = BumpedEdge();
            new PosterizeStep { colors = 2, minRegionPercent = 0f }.Apply(raw);
            Assert.AreEqual(raw.regions[5 * 16 + 0], raw.regions[5 * 16 + 9], "control: bump is red without smoothing");

            StyleBuffer smooth = BumpedEdge();
            new PosterizeStep { colors = 2, minRegionPercent = 0f, smoothPercent = 100f * 2f / 16f }.Apply(smooth);
            Assert.AreEqual(smooth.regions[5 * 16 + 12], smooth.regions[5 * 16 + 9]);
            Assert.AreNotEqual(smooth.regions[5 * 16 + 0], smooth.regions[5 * 16 + 12]);
        }

        [Test]
        public void ColorsBelowTwoClampedToTwo()
        {
            StyleBuffer buf = NoisyTwoTone(16, 8);
            new PosterizeStep { colors = 1, minRegionPercent = 0f }.Apply(buf);
            Assert.AreEqual(2, new HashSet<Vector4>(buf.pixels).Count);
        }
    }
}
