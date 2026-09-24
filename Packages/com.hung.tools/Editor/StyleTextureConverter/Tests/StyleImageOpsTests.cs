using NUnit.Framework;

namespace StyleTextureConverter.Tests
{
    public sealed class StyleImageOpsTests
    {
        [Test]
        public void PercentUsesLongSideNotWidth()
        {
            var buf = new StyleBuffer(10, 200, 1f);
            Assert.AreEqual(20f, buf.PercentToTexels(10f), 1e-5f);
        }

        [Test]
        public void RegionsNullByDefault()
        {
            Assert.IsNull(new StyleBuffer(2, 2, 1f).regions);
        }

        [Test]
        public void DistanceCountsFromTarget()
        {
            float[] d = StyleImageOps.DistanceTo(new[] { true, false, false, false }, 4, 1);
            Assert.AreEqual(0f, d[0], 1e-5f);
            Assert.AreEqual(1f, d[1], 1e-5f);
            Assert.AreEqual(3f, d[3], 1e-5f);
        }

        [Test]
        public void DistanceWithoutTargetIsHuge()
        {
            float[] d = StyleImageOps.DistanceTo(new[] { false, false }, 2, 1);
            Assert.Greater(d[0], 1e6f);
        }

        [Test]
        public void ComponentsSplitSameClassAcrossGap()
        {
            // row: 0 0 -1 0 1 -> {0,0}, {0}, {1} = 3 components
            int[] labels = StyleImageOps.Components(new[] { 0, 0, -1, 0, 1 }, 5, 1, out int count);
            Assert.AreEqual(3, count);
            Assert.AreEqual(labels[0], labels[1]);
            Assert.AreEqual(-1, labels[2]);
            Assert.AreNotEqual(labels[1], labels[3]);
            Assert.AreNotEqual(labels[3], labels[4]);
        }

        [Test]
        public void ComponentsAreFourConnected()
        {
            // 2x2 diagonal: 0 -1 / -1 0 -> two components
            StyleImageOps.Components(new[] { 0, -1, -1, 0 }, 2, 2, out int count);
            Assert.AreEqual(2, count);
        }

        [Test]
        public void ErodeThenDilateRestoresThickBlock()
        {
            // 9x1: F F F F F T T T T -- erode 1 removes index 4, dilate 1 grows it back
            bool[] m = { true, true, true, true, true, false, false, false, false };
            bool[] e = StyleImageOps.Erode(m, 9, 1, 1f);
            Assert.IsFalse(e[4]);
            Assert.IsTrue(e[3]);
            bool[] d = StyleImageOps.Dilate(e, 9, 1, 1f);
            Assert.IsTrue(d[4]);
            Assert.IsFalse(d[5]);
        }

        [Test]
        public void RadiusBelowOneIsNoOp()
        {
            bool[] m = { true, false };
            CollectionAssert.AreEqual(m, StyleImageOps.Erode(m, 2, 1, 0.5f));
            CollectionAssert.AreEqual(m, StyleImageOps.Dilate(m, 2, 1, 0.5f));
        }
    }
}
