using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Hung.DesignPattern.Tests
{
    public class GenericStateMachineTests
    {
        private sealed class State : IState<string>
        {
            public int Enters, Exits, Updates;
            public Action OnEnter, OnExit;
            public void Enter() { Enters++; OnEnter?.Invoke(); }
            public void Exit() { Exits++; OnExit?.Invoke(); }
            public bool Update() { Updates++; return true; }
            public bool FixedUpdate() => true;
        }

        [Test]
        public void RunningStart_AndStoppedStop_AreHarmless()
        {
            var machine = new StateMachine<string>();
            var idle = new State();
            Assert.AreEqual(StateStatus.Success, machine.Register("idle", idle));
            machine.Start("idle");
            Assert.AreEqual(StateStatus.Success, machine.Start("unknown"));
            Assert.AreEqual(1, idle.Enters);
            machine.Stop();
            machine.Stop();
            Assert.AreEqual(1, idle.Exits);
            Assert.IsFalse(machine.IsStarted);
        }

        [Test]
        public void Replacement_IsInactiveUntilSameKeyTransition()
        {
            var machine = new StateMachine<string>();
            var old = new State();
            var next = new State();
            machine.Register("idle", old);
            machine.Start("idle");
            Assert.AreEqual(StateStatus.DuplicateKey, machine.Register("idle", next));
            Assert.AreEqual(StateStatus.Success, machine.Replace("idle", next));
            Assert.AreSame(old, machine.CurrentState);
            machine.ChangeState("idle");
            Assert.AreEqual(1, old.Exits);
            Assert.AreEqual(1, next.Enters);
            Assert.AreSame(next, machine.CurrentState);
        }

        [Test]
        public void Validation_DoesNotChangeLifecycle()
        {
            var machine = new StateMachine<string>();
            var idle = new State();
            Assert.AreEqual(StateStatus.MissingKey, machine.Start("unknown"));
            Assert.IsFalse(machine.IsStarted);
            Assert.AreEqual(StateStatus.NullState, machine.Register("idle", null));
            Assert.AreEqual(StateStatus.MissingKey, machine.Replace("idle", idle));
            machine.Register("idle", idle);
            machine.Start("idle");
            Assert.AreEqual(StateStatus.MissingKey, machine.ChangeState("unknown"));
            Assert.AreEqual(0, idle.Exits);
            Assert.AreSame(idle, machine.CurrentState);
        }

        [Test]
        public void EnterTransition_PublishesFinalKey_ExitTransitionIsBusy()
        {
            var machine = new StateMachine<string>();
            var a = new State();
            var b = new State();
            StateStatus nestedExit = StateStatus.Success;
            a.OnEnter = () => machine.ChangeState("b");
            a.OnExit = () => nestedExit = machine.ChangeState("a");
            machine.Register("a", a);
            machine.Register("b", b);
            machine.Start("a");
            Assert.AreEqual(StateStatus.Busy, nestedExit);
            Assert.IsTrue(machine.TryGetCurrentKey(out var key));
            Assert.AreEqual("b", key);
            Assert.AreSame(b, machine.CurrentState);
        }

        [Test]
        public void CallbackException_IsNotReportedAsValidationRollback()
        {
            var machine = new StateMachine<string>();
            var state = new State { OnEnter = () => throw new InvalidOperationException("product-enter") };
            machine.Register("idle", state);
            Assert.Throws<InvalidOperationException>(() => machine.Start("idle"));
            Assert.IsTrue(machine.IsStarted);
            Assert.AreSame(state, machine.CurrentState);
            machine.Stop();
            Assert.IsFalse(machine.IsStarted);
        }
    }
}
