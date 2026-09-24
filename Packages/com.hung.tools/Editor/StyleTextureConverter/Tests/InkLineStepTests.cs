using NUnit.Framework;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class InkLineStepTests
    {
        static readonly Color Red = Color.red;

        static StyleBuffer Row(int w, params float[] alpha)
        {
            var buf = new StyleBuffer(w, 1, 1f);
            for (int x = 0; x < w; x++)
            {
                buf.pixels[x] = new Vector4(0f, 1f, 0f, alpha.Length > 0 ? alpha[x] : 1f);
            }
            return buf;
        }

        static bool Inked(StyleBuffer buf, int i) => buf.pixels[i].x > 0.99f && buf.pixels[i].y < 0.01f;

        [Test]
        public void InteriorInkOnlyOnSeam()
        {
            StyleBuffer buf = Row(6);
            buf.regions = new[] { 0, 0, 0, 1, 1, 1 };
            new InkLineStep { ink = Red, outlinePercent = 0f, interiorPercent = 100f * 2f / 6f }.Apply(buf);
            Assert.IsFalse(Inked(buf, 1));
            Assert.IsTrue(Inked(buf, 2));
            Assert.IsTrue(Inked(buf, 3));
            Assert.IsFalse(Inked(buf, 4));
        }

        [Test]
        public void InteriorWidthGrowsWithPercent()
        {
            StyleBuffer buf = Row(10);
            buf.regions = new[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 };
            new InkLineStep { ink = Red, outlinePercent = 0f, interiorPercent = 40f }.Apply(buf); // 4 texels
            Assert.IsFalse(Inked(buf, 2));
            Assert.IsTrue(Inked(buf, 3));
            Assert.IsTrue(Inked(buf, 6));
            Assert.IsFalse(Inked(buf, 7));
        }

        [Test]
        public void OutlineWidthScalesWithLongSide()
        {
            // 10 wide: T O O O O O O O O T ; 20% of 10 = 2 texels
            StyleBuffer small = Row(10, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0);
            new InkLineStep { ink = Red, outlinePercent = 20f, interiorPercent = 0f }.Apply(small);
            Assert.IsTrue(Inked(small, 1));
            Assert.IsTrue(Inked(small, 2));
            Assert.IsFalse(Inked(small, 3));
            Assert.IsTrue(Inked(small, 8));

            // same shape at 2x resolution -> 4 texels
            var alpha = new float[20];
            for (int i = 2; i < 18; i++) alpha[i] = 1f;
            StyleBuffer big = Row(20, alpha);
            new InkLineStep { ink = Red, outlinePercent = 20f, interiorPercent = 0f }.Apply(big);
            Assert.IsTrue(Inked(big, 5));
            Assert.IsFalse(Inked(big, 6));
        }

        [Test]
        public void NullRegionsGivesOutlineOnly()
        {
            StyleBuffer buf = Row(10, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0);
            new InkLineStep { ink = Red, outlinePercent = 10f, interiorPercent = 50f }.Apply(buf);
            Assert.IsTrue(Inked(buf, 1));
            Assert.IsFalse(Inked(buf, 4));
        }

        [Test]
        public void ClearTexelsNotInkedFringeInkedAlphaUntouched()
        {
            StyleBuffer buf = Row(4, 0f, 1f, 1f, 0.3f);
            buf.regions = new[] { -1, 0, 1, -1 };
            new InkLineStep { ink = Red, outlinePercent = 50f, interiorPercent = 50f }.Apply(buf);
            Assert.IsFalse(Inked(buf, 0));
            Assert.IsTrue(Inked(buf, 3)); // soft fringe (0 < alpha < 0.5) takes ink so no background RGB bleeds
            Assert.AreEqual(0f, buf.pixels[0].w);
            Assert.AreEqual(1f, buf.pixels[1].w);
            Assert.AreEqual(0.3f, buf.pixels[3].w, 1e-6f);
        }

        [Test]
        public void SoftFringeTexelsGetInkKeepAlpha()
        {
            StyleBuffer buf = Row(4, 0f, 0.3f, 1f, 1f);
            new InkLineStep { ink = Red, outlinePercent = 25f, interiorPercent = 0f }.Apply(buf);
            Assert.IsFalse(Inked(buf, 0));
            Assert.IsTrue(Inked(buf, 1));
            Assert.AreEqual(0.3f, buf.pixels[1].w, 1e-6f);
        }

        [Test]
        public void AntiAliasFadesOneTexelBeyondWidth()
        {
            // T O O O ; outline 25% of 4 = 1 texel -> idx1 full; idx2 (d=2) half with AA, none without
            StyleBuffer hard = Row(4, 0f, 1f, 1f, 1f);
            new InkLineStep { ink = Red, outlinePercent = 25f, interiorPercent = 0f }.Apply(hard);
            Assert.AreEqual(0f, hard.pixels[2].x, 1e-5f);

            StyleBuffer soft = Row(4, 0f, 1f, 1f, 1f);
            new InkLineStep { ink = Red, outlinePercent = 25f, interiorPercent = 0f, antiAlias = true }.Apply(soft);
            Assert.IsTrue(Inked(soft, 1));
            Assert.AreEqual(0.5f, soft.pixels[2].x, 1e-5f);
            Assert.AreEqual(0.5f, soft.pixels[2].y, 1e-5f);
            Assert.AreEqual(0f, soft.pixels[3].x, 1e-5f);
        }

        [Test]
        public void DefaultInkIsPetInk()
        {
            Color c = new InkLineStep().ink;
            Assert.AreEqual(0x27 / 255f, c.r, 1e-5f);
            Assert.AreEqual(0x02 / 255f, c.g, 1e-5f);
            Assert.AreEqual(0f, c.b, 1e-5f);
        }
    }
}
