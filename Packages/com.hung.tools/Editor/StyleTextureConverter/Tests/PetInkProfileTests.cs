using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class PetInkProfileTests
    {
        const string Path = "Packages/com.hung.tools/Editor/StyleTextureConverter/Profiles/StyleProfile_PetInk.asset";

        static StyleProfile Load()
        {
            var p = AssetDatabase.LoadAssetAtPath<StyleProfile>(Path);
            Assert.IsNotNull(p, "profile asset missing at " + Path);
            return p;
        }

        [Test]
        public void HasFourStepsInOrder()
        {
            StyleProfile p = Load();
            Assert.AreEqual("_PetInk", p.outputSuffix);
            Assert.AreEqual(4, p.steps.Count);
            Assert.IsInstanceOf<BackgroundCutStep>(p.steps[0]);
            Assert.IsInstanceOf<PosterizeStep>(p.steps[1]);
            Assert.IsInstanceOf<AdjustStep>(p.steps[2]);
            Assert.IsInstanceOf<InkLineStep>(p.steps[3]);
        }

        [Test]
        public void SyntheticCardGetsTransparentCornersAndInkEdge()
        {
            // 64x64 "card": radial rays background + light disc with a white sticker ring
            const int N = 64;
            var buf = new StyleBuffer(N, N, 1f);
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = x - 31.5f, dy = y - 31.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    bool ray = ((int)((Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI) * 12f)) % 2 == 0;
                    Vector4 c = r < 16f ? new Vector4(0.9f, 0.6f, 0.2f, 1f)
                        : r < 19f ? new Vector4(1f, 1f, 1f, 1f)
                        : ray ? new Vector4(0.2f, 0.3f, 0.6f, 1f) : new Vector4(0.25f, 0.35f, 0.65f, 1f);
                    buf.pixels[y * N + x] = c;
                }
            }

            StyleTextureProcessor.RunSteps(buf, Load().steps);

            Assert.AreEqual(0f, buf.pixels[0].w, "corner should be cut");
            Assert.AreEqual(1f, buf.pixels[32 * N + 32].w, "centre should stay");
            int edgeX = -1;
            for (int x = 0; x < N; x++)
            {
                if (buf.pixels[32 * N + x].w > 0.5f) { edgeX = x; break; }
            }
            Assert.GreaterOrEqual(edgeX, 0);
            Vector4 edge = buf.pixels[32 * N + edgeX];
            Assert.AreEqual(0x27 / 255f, edge.x, 0.02f);
            Assert.AreEqual(0x02 / 255f, edge.y, 0.02f);
        }
    }
}
