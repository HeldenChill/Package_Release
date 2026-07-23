using NUnit.Framework;
using Hung.DesignPattern;

namespace Hung.DesignPattern.Tests
{
    public class StateMachineTests
    {
        private class RecordingState : BaseState
        {
            public readonly STATE id;
            public int enterCount, exitCount;

            public RecordingState(STATE id) { this.id = id; }
            public override STATE Id => id;
            public override void Enter() => enterCount++;
            public override bool Update() => true;
            public override void Exit() => exitCount++;
        }

        [Test]
        public void Start_ThenChangeState_CallsExitOnOldThenEnterOnNew()
        {
            var sm = new StateMachine();
            var idle = new RecordingState(STATE.IDLE);
            var move = new RecordingState(STATE.MOVE);
            sm.AddState(STATE.IDLE, idle);
            sm.AddState(STATE.MOVE, move);
            sm.Start(STATE.IDLE);

            sm.ChangeState(STATE.MOVE);

            Assert.AreEqual(1, idle.exitCount);
            Assert.AreEqual(1, move.enterCount);
            Assert.AreEqual(STATE.MOVE, sm.CurrentState);
        }

        [Test]
        public void ChangeState_ToSameState_ReEntersRatherThanNoOp()
        {
            // Actual behavior verified by reading StateMachine.ChangeState: it does not
            // special-case same-state, so re-entering the current state calls Exit then
            // Enter again on the same instance.
            var sm = new StateMachine();
            var idle = new RecordingState(STATE.IDLE);
            sm.AddState(STATE.IDLE, idle);
            sm.Start(STATE.IDLE);

            sm.ChangeState(STATE.IDLE);

            Assert.AreEqual(1, idle.exitCount);
            Assert.AreEqual(2, idle.enterCount);
        }

        [Test]
        public void Start_CallsEnterOnInitialState()
        {
            var sm = new StateMachine();
            var idle = new RecordingState(STATE.IDLE);
            sm.AddState(STATE.IDLE, idle);

            sm.Start(STATE.IDLE);

            Assert.AreEqual(1, idle.enterCount);
        }

        [Test]
        public void Start_UnregisteredState_Throws()
        {
            // Start() indexes the dictionary directly (states[id]), unlike ChangeState()
            // which guards with ContainsKey - characterizing that asymmetry.
            var sm = new StateMachine();

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => sm.Start(STATE.IDLE));
        }

        [Test]
        public void ChangeState_UnregisteredState_NoOpDoesNotThrow()
        {
            var sm = new StateMachine();
            var idle = new RecordingState(STATE.IDLE);
            sm.AddState(STATE.IDLE, idle);
            sm.Start(STATE.IDLE);

            Assert.DoesNotThrow(() => sm.ChangeState(STATE.MOVE));
            Assert.AreEqual(STATE.IDLE, sm.CurrentState, "unregistered target must not change currentState");
        }
    }
}
