using NUnit.Framework;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class KeyExtractStepTests
    {
        [Test]
        public void GreenChannelNormalizesOpaqueMinMaxToZeroOne()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(3, 1,
                new Color32(0, 51, 0, 255), new Color32(0, 153, 0, 255), new Color32(0, 102, 0, 255));
            new KeyExtractStep { channel = KeyChannel.G, autoNormalize = true }.Apply(buf);
            Assert.AreEqual(0f, buf.key[0], 1e-5f);
            Assert.AreEqual(1f, buf.key[1], 1e-5f);
            Assert.AreEqual(0.5f, buf.key[2], 1e-5f);
        }

        [Test]
        public void TexelsBelowAlphaThresholdDoNotSetRange()
        {
            // Transparent texel with G=255 must not become the max.
            StyleBuffer buf = StyleTestUtil.Buffer(3, 1,
                new Color32(0, 0, 0, 255), new Color32(0, 100, 0, 255), new Color32(0, 255, 0, 10));
            new KeyExtractStep { channel = KeyChannel.G, autoNormalize = true }.Apply(buf);
            Assert.AreEqual(1f, buf.key[1], 1e-5f);
            Assert.AreEqual(1f, buf.key[2], 1e-5f); // clamped
        }

        [Test]
        public void FlatKeyKeepsRawValuesWithoutNaN()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(2, 1, new Color32(0, 128, 0, 255), new Color32(0, 128, 0, 255));
            new KeyExtractStep { channel = KeyChannel.G, autoNormalize = true }.Apply(buf);
            Assert.AreEqual(128f / 255f, buf.key[0], 1e-5f);
            Assert.IsFalse(float.IsNaN(buf.key[1]));
        }

        [Test]
        public void LuminanceWithoutNormalizeMatchesRec709()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(255, 0, 0, 255));
            new KeyExtractStep { channel = KeyChannel.Luminance, autoNormalize = false }.Apply(buf);
            Assert.AreEqual(0.2126f, buf.key[0], 1e-4f);
        }

        [Test]
        public void PixelsAndAlphaUntouched()
        {
            var px = new Color32(10, 20, 30, 40);
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, px);
            Vector4 before = buf.pixels[0];
            new KeyExtractStep { channel = KeyChannel.Max }.Apply(buf);
            Assert.AreEqual(before, buf.pixels[0]);
        }
    }
}
