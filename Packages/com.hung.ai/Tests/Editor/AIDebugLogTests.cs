using NUnit.Framework;
namespace Hung.AI.Editor.Tests
{
    public class AIDebugLogTests
    {
        [Test]
        public void Record_RunningThenSuccess_LogsTwoLines()
        {
            var log = new AIDebugLog();
            log.Record("m", "Move To", "Chase", NodeStatus.Running, 1);
            log.Record("m", "Move To", "Chase", NodeStatus.Running, 2);
            log.Record("m", "Move To", "Chase", NodeStatus.Success, 3);
            Assert.AreEqual(2, log.Entries.Count);
            Assert.AreEqual("Chase: Move To.OnStart", log.Entries[0].Text);
            Assert.AreEqual(1, log.Entries[0].Tick);
            Assert.AreEqual(AIDebugLogKind.Run, log.Entries[0].Kind);
            Assert.AreEqual("Move To " + AIGlyphs.Arrow + " Success, OnEnd", log.Entries[1].Text);
            Assert.AreEqual(3, log.Entries[1].Tick);
            Assert.AreEqual(AIDebugLogKind.Success, log.Entries[1].Kind);
        }

        [Test]
        public void Record_SingleTickResult_IsNotLogged()
        {
            var log = new AIDebugLog();
            log.Record("c", "Is Set", "Chase", NodeStatus.Success, 1);
            log.Record("c", "Is Set", "Chase", NodeStatus.Failure, 2);
            Assert.AreEqual(0, log.Entries.Count);
            Assert.AreEqual(2, log.Tick);
        }

        [Test]
        public void Record_AbortThenNewBranchSameFrame_NamesTheNewBranch()
        {
            var log = new AIDebugLog();
            log.Record("m", "Move To", "Chase", NodeStatus.Running, 1);
            log.Record("m", "Move To", "Chase", NodeStatus.Aborted, 2);
            log.Record("inv", "Investigate", "Selector", NodeStatus.Running, 2);
            Assert.AreEqual(3, log.Entries.Count);
            Assert.AreEqual("ABORT Chase " + AIGlyphs.Arrow + " Move To.OnAbort, OnEnd  (now Investigate)", log.Entries[1].Text);
            Assert.AreEqual(AIDebugLogKind.Abort, log.Entries[1].Kind);
            Assert.AreEqual("Selector: Investigate.OnStart", log.Entries[2].Text);
        }

        [Test]
        public void Record_NodeWithoutParent_HasNoPrefix()
        {
            var log = new AIDebugLog();
            log.Record("r", "Parallel", string.Empty, NodeStatus.Running, 1);
            log.Record("r", "Parallel", null, NodeStatus.Aborted, 2);
            Assert.AreEqual("Parallel.OnStart", log.Entries[0].Text);
            Assert.AreEqual("ABORT Parallel.OnAbort, OnEnd", log.Entries[1].Text);
        }

        [Test]
        public void Info_KeepsOnlyTheLastCapacityLines()
        {
            var log = new AIDebugLog();
            for (int i = 0; i < AIDebugLog.Capacity + 50; i++) log.Info("line " + i);
            Assert.AreEqual(AIDebugLog.Capacity, log.Entries.Count);
            Assert.AreEqual("line 50", log.Entries[0].Text);
            int version = log.Version;
            log.Clear();
            Assert.AreEqual(0, log.Entries.Count);
            Assert.AreEqual(0, log.Tick);
            Assert.Greater(log.Version, version);
        }
    }
}
