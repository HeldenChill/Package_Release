using NUnit.Framework;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class GradientRampStepTests
    {
        static Gradient RedToBlue()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.blue, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        [Test]
        public void KeyEndpointsMapToGradientEndpoints()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(2, 1, new Color32(0, 0, 0, 77), new Color32(0, 0, 0, 200));
            buf.key = new[] { 0f, 1f };
            new GradientRampStep { ramp = RedToBlue() }.Apply(buf);
            Assert.AreEqual(new Vector4(1f, 0f, 0f, 77f / 255f), buf.pixels[0]);
            Assert.AreEqual(new Vector4(0f, 0f, 1f, 200f / 255f), buf.pixels[1]);
        }

        [Test]
        public void NullKeyFallsBackToLuminance()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(2, 1, new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255));
            new GradientRampStep { ramp = RedToBlue() }.Apply(buf);
            Assert.AreEqual(1f, buf.pixels[0].x, 1e-4f);  // black -> red
            Assert.AreEqual(1f, buf.pixels[1].z, 1e-4f);  // white -> blue
        }
    }
}
