using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace StyleTextureConverter.Tests
{
    public sealed class BackgroundCutStepTests
    {
        const int N = 32;

        static StyleBuffer Grey(float bg = 0.5f, float alpha = 1f)
        {
            var buf = new StyleBuffer(N, N, 1f);
            for (int i = 0; i < buf.pixels.Length; i++) buf.pixels[i] = new Vector4(bg, bg, bg, alpha);
            return buf;
        }

        /// Grey background, black disc at the centre.
        static StyleBuffer Disc(float radius)
        {
            StyleBuffer buf = Grey();
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = x - (N - 1) / 2f, dy = y - (N - 1) / 2f;
                    if (dx * dx + dy * dy <= radius * radius) Set(buf, x, y, 0f);
                }
            }
            return buf;
        }

        static void Block(StyleBuffer buf, int x0, int y0, int x1, int y1, float v, float alpha = 1f)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    buf.pixels[y * N + x] = new Vector4(v, v, v, alpha);
        }

        static void Set(StyleBuffer buf, int x, int y, float v) => buf.pixels[y * N + x] = new Vector4(v, v, v, 1f);
        static float A(StyleBuffer buf, int x, int y) => buf.pixels[y * N + x].w;

        static BackgroundCutStep Plain() => new BackgroundCutStep
        {
            gradientThreshold = 0.35f, openPercent = 0f, shrinkPercent = 0f, keepLargestOnly = true, fillHoles = true,
            featherPercent = 0f
        };

        [Test]
        public void GradientReadsOneOnHardStep()
        {
            StyleBuffer buf = StyleTestUtil.Buffer(3, 1,
                new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255));
            float[] g = BackgroundCutStep.Gradient(buf);
            Assert.AreEqual(1f, g[1], 1e-4f);
        }

        [Test]
        public void DiscKeptBorderCut()
        {
            StyleBuffer buf = Disc(8f);
            Plain().Apply(buf);
            Assert.AreEqual(0f, A(buf, 0, 0));
            Assert.AreEqual(0f, A(buf, N - 1, N / 2));
            Assert.AreEqual(1f, A(buf, N / 2, N / 2));
        }

        [Test]
        public void RgbUntouched()
        {
            StyleBuffer buf = Disc(8f);
            Plain().Apply(buf);
            Assert.AreEqual(0.5f, buf.pixels[0].x, 1e-5f);
            Assert.AreEqual(0f, buf.pixels[(N / 2) * N + N / 2].x, 1e-5f);
        }

        [Test]
        public void LargestOnlyDropsDetachedBlock()
        {
            StyleBuffer buf = Disc(8f);
            Block(buf, 3, 3, 5, 5, 0f); // separate 3x3 black block in a corner
            Plain().Apply(buf);
            Assert.AreEqual(0f, A(buf, 4, 4));
            Assert.AreEqual(1f, A(buf, N / 2, N / 2));
        }

        [Test]
        public void LargestOffKeepsDetachedBlock()
        {
            StyleBuffer buf = Disc(8f);
            Block(buf, 3, 3, 5, 5, 0f);
            var step = Plain();
            step.keepLargestOnly = false;
            step.Apply(buf);
            Assert.AreEqual(1f, A(buf, 4, 4));
        }

        [Test]
        public void OpenDetachesGluedLine()
        {
            StyleBuffer buf = Disc(8f);
            int y = N / 2;
            // 2-texel line off the disc (a 1-texel line reads passable); the Sobel halo makes it 4 thick
            Block(buf, 24, y, 28, y + 1, 0f);
            var withoutOpen = Plain();
            StyleBuffer control = Disc(8f);
            Block(control, 24, y, 28, y + 1, 0f);
            withoutOpen.Apply(control);
            Assert.AreEqual(1f, A(control, 28, y), "control: line stays without open");

            var step = Plain();
            step.openPercent = 100f * 2.5f / N; // radius 2.5 texels removes a 4-thick band
            step.Apply(buf);
            Assert.AreEqual(0f, A(buf, 28, y));
            Assert.AreEqual(1f, A(buf, N / 2, y));
        }

        [Test]
        public void EnclosedBackgroundColourStaysSubject()
        {
            StyleBuffer buf = Disc(10f);
            Block(buf, 14, 14, 17, 17, 0.5f); // grey pocket, same colour as background, ringed by the disc
            Plain().Apply(buf);
            Assert.AreEqual(1f, A(buf, 15, 15));
        }

        [Test]
        public void FillHolesFillsEnclosedPocket()
        {
            // 5x5 mask: 3x3 ring at 1..3 with an empty centre
            bool[] fg = new bool[25];
            for (int y = 1; y <= 3; y++) for (int x = 1; x <= 3; x++) fg[y * 5 + x] = true;
            fg[2 * 5 + 2] = false;
            bool[] filled = BackgroundCutStep.FillHoles(fg, 5, 5);
            Assert.IsTrue(filled[2 * 5 + 2]);
            Assert.IsFalse(filled[0]);
        }

        [Test]
        public void ShrinkMovesEdgeInward()
        {
            StyleBuffer a = Grey();
            StyleBuffer b = Grey();
            Block(a, 10, 10, 21, 21, 0f); // square: horizontal distance on the centre row is exact
            Block(b, 10, 10, 21, 21, 0f);
            Plain().Apply(a);
            var step = Plain();
            step.shrinkPercent = 100f * 2f / N; // 2 texels
            step.Apply(b);
            int row = N / 2, widthA = 0, widthB = 0;
            for (int x = 0; x < N; x++)
            {
                if (A(a, x, row) > 0.5f) widthA++;
                if (A(b, x, row) > 0.5f) widthB++;
            }
            Assert.AreEqual(widthA - 4, widthB); // 2 texels off each side
        }

        [Test]
        public void NoBackgroundReachedWarnsAndKeepsAlpha()
        {
            // period-3 ramp 0, 0.5, 1: every texel (border included) reads gradient >= 0.5
            StyleBuffer buf = Grey();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) Set(buf, x, y, (x % 3) * 0.5f);
            LogAssert.Expect(LogType.Warning, new Regex("no background reached from border"));
            Plain().Apply(buf);
            Assert.AreEqual(1f, A(buf, 0, 0));
        }

        [Test]
        public void CutNeverBlanksImage_RestoresAlphaAndWarns()
        {
            StyleBuffer buf = Disc(3f);
            var step = Plain();
            step.shrinkPercent = 50f; // 16 texels, eats the whole disc
            LogAssert.Expect(LogType.Warning, new Regex("shrinkPercent"));
            step.Apply(buf);
            Assert.AreEqual(1f, A(buf, 0, 0));
            Assert.AreEqual(1f, A(buf, N / 2, N / 2));
        }

        [Test]
        public void AlreadyCutArtKeepsCut()
        {
            // re-running on our own output: transparent surround (grey RGB), opaque black centre
            StyleBuffer buf = Grey(0.5f, 0f);
            Block(buf, 8, 8, 23, 23, 0f);
            Plain().Apply(buf);
            Assert.AreEqual(0f, A(buf, 0, 0));
            Assert.AreEqual(1f, A(buf, 16, 16));
        }

        [Test]
        public void TransparentHoleSurvives()
        {
            StyleBuffer buf = Grey();
            Block(buf, 8, 8, 23, 23, 0f);           // opaque black square
            Block(buf, 14, 14, 17, 17, 0f, 0f);     // transparent hole inside it
            Plain().Apply(buf);
            Assert.AreEqual(0f, A(buf, 15, 15));
            Assert.AreEqual(1f, A(buf, 10, 10));
            Assert.AreEqual(0f, A(buf, 0, 0));
        }

        /// Grey background, black disc whose edge is a linear ramp 4% of the side wide.
        static StyleBuffer SoftDisc(int size)
        {
            var buf = new StyleBuffer(size, size, 1f);
            float radius = size * 0.25f, ramp = size * 0.04f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - (size - 1) / 2f, dy = y - (size - 1) / 2f;
                    float t = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - radius) / ramp);
                    float v = 0.5f * t;
                    buf.pixels[y * size + x] = new Vector4(v, v, v, 1f);
                }
            }
            return buf;
        }

        static float Coverage(StyleBuffer buf)
        {
            int n = 0;
            foreach (Vector4 p in buf.pixels) if (p.w > 0.5f) n++;
            return (float)n / buf.pixels.Length;
        }

        [Test]
        public void WorkingSizeMakesCutResolutionIndependent()
        {
            BackgroundCutStep Step(int working) => new BackgroundCutStep
            {
                gradientThreshold = 0.15f, openPercent = 0f, shrinkPercent = 0f,
                keepLargestOnly = true, fillHoles = true, workingSize = working, featherPercent = 0f
            };

            // full-res control: the soft ramp reads below threshold, the flood leaks through the disc
            StyleBuffer control = SoftDisc(512);
            LogAssert.Expect(LogType.Warning, new Regex("lower gradientThreshold"));
            Step(512).Apply(control);
            Assert.AreEqual(1f, Coverage(control), 1e-6f);

            StyleBuffer small = SoftDisc(128);
            StyleBuffer big = SoftDisc(512);
            Step(64).Apply(small);
            Step(64).Apply(big);
            // disc ~0.196 of the canvas; ramp + Sobel halo stay subject, so up to ~0.3
            Assert.That(Coverage(big), Is.InRange(0.15f, 0.35f));
            Assert.AreEqual(Coverage(small), Coverage(big), 0.02f);
            Assert.AreEqual(1f, big.pixels[256 * 512 + 256].w);
            Assert.AreEqual(0f, big.pixels[0].w);
        }

        [Test]
        public void FeatherGivesSoftEdge()
        {
            StyleBuffer buf = Disc(8f);
            var step = Plain();
            step.featherPercent = 100f * 2f / N; // 2 texels
            step.Apply(buf);
            int partial = 0;
            foreach (Vector4 p in buf.pixels) if (p.w > 0.01f && p.w < 0.99f) partial++;
            Assert.Greater(partial, 0);
            Assert.AreEqual(1f, A(buf, N / 2, N / 2), 1e-5f);
            Assert.AreEqual(0f, A(buf, 0, 0), 1e-5f);
        }

        [Test]
        public void FeatherKeepsEdgePosition()
        {
            StyleBuffer hard = Disc(8f);
            StyleBuffer soft = Disc(8f);
            Plain().Apply(hard);
            var step = Plain();
            step.featherPercent = 100f * 2f / N;
            step.Apply(soft);
            int row = N / 2, hardW = 0, softW = 0;
            for (int x = 0; x < N; x++)
            {
                if (A(hard, x, row) > 0.5f) hardW++;
                if (A(soft, x, row) >= 0.5f) softW++;
            }
            Assert.AreEqual(hardW, softW, 1);
        }

        [Test]
        public void FeatherZeroKeepsHardEdge()
        {
            StyleBuffer buf = Disc(8f);
            Plain().Apply(buf);
            foreach (Vector4 p in buf.pixels) Assert.IsTrue(p.w == 0f || p.w == 1f);
        }

        [Test]
        public void ClearsRegions()
        {
            StyleBuffer buf = Disc(8f);
            buf.regions = new int[N * N];
            Plain().Apply(buf);
            Assert.IsNull(buf.regions);
        }
    }
}
