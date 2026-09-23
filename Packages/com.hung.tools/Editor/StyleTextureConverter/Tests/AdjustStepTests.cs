using NUnit.Framework;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class AdjustStepTests
    {
        [Test]
        public void DefaultsAreIdentity()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(40, 120, 200, 90));
            Vector4 before = buf.pixels[0];
            new AdjustStep().Apply(buf);
            Assert.That(Vector4.Distance(before, buf.pixels[0]), Is.LessThan(1e-5f));
        }

        [Test]
        public void ZeroSaturationGivesGray()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(255, 0, 0, 255));
            new AdjustStep { saturation = 0f }.Apply(buf);
            Assert.AreEqual(0.2126f, buf.pixels[0].x, 1e-4f);
            Assert.AreEqual(0.2126f, buf.pixels[0].y, 1e-4f);
            Assert.AreEqual(0.2126f, buf.pixels[0].z, 1e-4f);
        }

        [Test]
        public void BrightnessAddsAndKeepsAlpha()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(0, 0, 0, 50));
            new AdjustStep { brightness = 0.25f }.Apply(buf);
            Assert.AreEqual(0.25f, buf.pixels[0].x, 1e-5f);
            Assert.AreEqual(50f / 255f, buf.pixels[0].w, 1e-5f);
        }
    }
}
