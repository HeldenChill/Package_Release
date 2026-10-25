using NUnit.Framework;

namespace Hung.DesignPattern.Tests
{
    public class FoundationCompatibilityTests
    {
        private enum ProductKey { None = -1, Idle = 1, Move = 2, Scare = 3, PrepareAttack = 11 }

        private sealed class Behavior : IState<ProductKey>
        {
            public int Enters, Exits;
            public void Enter() { Enters++; }
            public void Exit() { Exits++; }
            public bool Update() => true;
            public bool FixedUpdate() => true;
        }

        [Test]
        public void AttackVariants_ReplaceThenReenter_WithoutFrameworkEnumCasts()
        {
            var machine = new StateMachine<ProductKey>();
            var idle = new Behavior();
            var attackIdle = new Behavior();
            var move = new Behavior();
            machine.Register(ProductKey.Idle, idle);
            machine.Register(ProductKey.Move, move);
            machine.Start(ProductKey.Idle);
            Assert.IsTrue(machine.IsStarted);
            machine.Replace(ProductKey.Idle, attackIdle);
            Assert.AreSame(idle, machine.CurrentState);
            Assert.IsTrue(machine.TryGetCurrentKey(out var current));
            machine.ChangeState(current);
            Assert.AreEqual(1, idle.Exits);
            Assert.AreEqual(1, attackIdle.Enters);
            Assert.AreEqual("Behavior", machine.CurrentState.GetType().Name);
            Assert.AreEqual(3, (int)ProductKey.Scare);
            Assert.AreEqual(11, (int)ProductKey.PrepareAttack);
        }
    }
}
