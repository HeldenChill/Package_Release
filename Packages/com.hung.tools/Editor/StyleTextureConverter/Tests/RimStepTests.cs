using NUnit.Framework;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class RimStepTests
    {
        static readonly Color32 T = new Color32(0, 0, 0, 0);
        static readonly Color32 O = new Color32(0, 0, 0, 255);

        static RimStep Hard(float width) =>
            new RimStep { color = Color.red, widthTexels = width, softTexels = 0f, strength = 1f };

        [Test]
        public void DistanceCountsTexelsFromTransparentEdge()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(5, 1, T, O, O, O, O);
            float[] d = RimStep.DistanceToTransparent(buf);
            Assert.AreEqual(0f, d[0], 1e-5f);
            Assert.AreEqual(1f, d[1], 1e-5f);
            Assert.AreEqual(4f, d[4], 1e-5f);
        }

        [Test]
        public void ColoursOnlyTexelsWithinWidth()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(5, 1, T, O, O, O, O);
            Hard(2f).Apply(buf);
            Assert.AreEqual(1f, buf.pixels[1].x, 1e-5f);
            Assert.AreEqual(1f, buf.pixels[2].x, 1e-5f);
            Assert.AreEqual(0f, buf.pixels[3].x, 1e-5f);
            Assert.AreEqual(0f, buf.pixels[0].x, 1e-5f); // transparent texel untouched
        }

        [Test]
        public void PreviewScaleShrinksReach()
        {
            Texture2D tex = StyleTestUtil.MakeTexture(5, 1, T, O, O, O, O);
            try
            {
                StyleBuffer buf = StyleTextureProcessor.ToBuffer(tex, 0.5f);
                Hard(2f).Apply(buf);
                Assert.AreEqual(1f, buf.pixels[1].x, 1e-5f);
                Assert.AreEqual(0f, buf.pixels[2].x, 1e-5f);
            }
            finally
            {
                StyleTestUtil.Destroy(tex);
            }
        }

        [Test]
        public void SoftEdgeFadesLinearly()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(5, 1, T, O, O, O, O);
            new RimStep { color = Color.red, widthTexels = 4f, softTexels = 2f, strength = 1f }.Apply(buf);
            Assert.AreEqual(1f, buf.pixels[2].x, 1e-5f);   // d=2 <= width-soft
            Assert.AreEqual(0.5f, buf.pixels[3].x, 1e-5f); // d=3 halfway through fade
            Assert.AreEqual(0f, buf.pixels[4].x, 1e-5f);   // d=4 == width
        }

        [Test]
        public void FullyOpaqueTextureGetsNoRim()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(3, 1, O, O, O);
            Hard(10f).Apply(buf);
            Assert.AreEqual(0f, buf.pixels[0].x);
            Assert.AreEqual(0f, buf.pixels[1].x);
        }

        [Test]
        public void AlphaPreserved()
        {
            var semi = new Color32(0, 0, 0, 180);
            StyleBuffer buf = StyleTestUtil.Buffer(2, 1, T, semi);
            Hard(2f).Apply(buf);
            Assert.AreEqual(180f / 255f, buf.pixels[1].w, 1e-5f);
        }
    }
}
