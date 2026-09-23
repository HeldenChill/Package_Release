using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class StyleTextureProcessorTests
    {
        static StyleProfile Profile(params StyleStep[] steps)
        {
            var profile = ScriptableObject.CreateInstance<StyleProfile>();
            profile.steps = new List<StyleStep>(steps);
            return profile;
        }

        [Test]
        public void EmptyProfileReturnsIdenticalPixels()
        {
            var px = new[] { new Color32(10, 20, 30, 40), new Color32(200, 150, 100, 255) };
            Texture2D source = StyleTestUtil.MakeTexture(2, 1, px);
            StyleProfile profile = Profile();
            Texture2D result = null;
            try
            {
                result = StyleTextureProcessor.Run(source, profile);
                CollectionAssert.AreEqual(px, result.GetPixels32());
            }
            finally
            {
                StyleTestUtil.Destroy(result);
                StyleTestUtil.Destroy(source);
                StyleTestUtil.Destroy(profile);
            }
        }

        [Test]
        public void DisabledStepIsNoOp()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(0, 0, 0, 255));
            StyleTextureProcessor.RunSteps(buf, new StyleStep[] { new SetRedStep { red = 1f, enabled = false } });
            Assert.AreEqual(0f, buf.pixels[0].x);
        }

        [Test]
        public void StepsRunInOrder()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(0, 0, 0, 255));
            StyleTextureProcessor.RunSteps(buf, new StyleStep[] { new SetRedStep { red = 1f }, new SetRedStep { red = 0.25f } });
            Assert.AreEqual(0.25f, buf.pixels[0].x, 1e-6f);
        }

        [Test]
        public void NullStepIsSkipped()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(0, 0, 0, 255));
            // Logs a warning; warnings do not fail NUnit runs, and this asmdef has no TestRunner ref for LogAssert.
            StyleTextureProcessor.RunSteps(buf, new StyleStep[] { null, new SetRedStep { red = 1f } });
            Assert.AreEqual(1f, buf.pixels[0].x, 1e-6f);
        }

        [Test]
        public void ThrowingStepIsWrappedWithIndexAndName()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(1, 1, new Color32(0, 0, 0, 255));
            var ex = Assert.Throws<StyleStepException>(() =>
                StyleTextureProcessor.RunSteps(buf, new StyleStep[] { new SetRedStep(), new ThrowStep() }));
            Assert.AreEqual(1, ex.index);
            StringAssert.Contains("Step 1 Thrower: boom", ex.Message);
        }

        [Test]
        public void DownscaleSmallSourceKeepsSizeAndScaleOne()
        {
            Texture2D source = StyleTestUtil.MakeTexture(4, 2, new Color32[8]);
            Texture2D small = null;
            try
            {
                small = StyleTextureProcessor.Downscale(source, 512, out float scale);
                Assert.AreNotSame(source, small);
                Assert.AreEqual(4, small.width);
                Assert.AreEqual(2, small.height);
                Assert.AreEqual(1f, scale);
            }
            finally
            {
                StyleTestUtil.Destroy(small);
                StyleTestUtil.Destroy(source);
            }
        }

        [Test]
        public void DownscaleLargeSourceHalvesAndReportsScale()
        {
            Texture2D source = StyleTestUtil.MakeTexture(1024, 2, new Color32[2048]);
            Texture2D small = null;
            try
            {
                small = StyleTextureProcessor.Downscale(source, 512, out float scale);
                Assert.AreEqual(512, small.width);
                Assert.AreEqual(1, small.height);
                Assert.AreEqual(0.5f, scale, 1e-6f);
            }
            finally
            {
                StyleTestUtil.Destroy(small);
                StyleTestUtil.Destroy(source);
            }
        }
    }
}
