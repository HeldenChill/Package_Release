using NUnit.Framework;

namespace Hung.Base.Tests
{
    public class LevelDataMarkPassedTests
    {
        [Test]
        public void MarkPassed_EmptyList_ExpandsAndReturnsTrueOnlyOnce()
        {
            var level = new GameData.LevelData();

            Assert.IsTrue(level.MarkPassed(3));
            Assert.AreEqual(4, level.PassLevels.Count);
            Assert.IsTrue(level.PassLevels[3]);
            Assert.IsFalse(level.PassLevels[2]);
            Assert.IsFalse(level.MarkPassed(3));
        }

        [Test]
        public void MarkPassed_NegativeLevel_ReturnsFalseAndLeavesListUntouched()
        {
            var level = new GameData.LevelData();

            Assert.IsFalse(level.MarkPassed(-1));
            Assert.AreEqual(0, level.PassLevels.Count);
        }
    }
}
