using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using Hung.Utilities.Timer;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Tutorial.Tests
{
    // Regression cover for flow audit 6 (BUG-261113-0e05, 292a). STimer only counts down in a real
    // frame loop, so a step with Delay > 0 stays "pending" for the whole EditMode test: that is the
    // window the 0e05 bug lived in.
    public sealed class TutorialStateMachineTests
    {
        private const int Level = 1;

        private TutorialDataSO _data;
        private STimer _timer;
        private TutorialRegistry _registry;
        private TutorialStateMachine _sm;
        private int _stepEntered;
        private int _branchPostActions;

        [SetUp]
        public void SetUp()
        {
            // NUnit reuses one fixture instance across tests.
            _stepEntered = 0;
            _branchPostActions = 0;
            _data = ScriptableObject.CreateInstance<TutorialDataSO>();
            _timer = TimerManager.Ins.PopSTimer();
            _registry = new TutorialRegistry();
            _registry.AddCondition("go", () => true);
            _registry.AddCondition("never", () => false);
            _registry.AddAction("entered", () => _stepEntered++);
            _registry.AddAction("branch-post", () => _branchPostActions++);
            _sm = new TutorialStateMachine(_registry, new TutorialUIController(), null, new TutorialStepContext());
            _sm.Configure(_data, _timer);
        }

        [TearDown]
        public void TearDown()
        {
            _timer.Stop();
            TimerManager.Ins.PushSTimer(_timer);
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void StopDuringDelay_ThenRun_AdvancesOnNextTick()
        {
            SetSteps(Level, DelayedStep());
            _sm.Run(Level);
            _sm.Tick();
            Assert.That(_stepEntered, Is.EqualTo(1), "first tick enters step 0 and starts its delay");

            _sm.Stop();
            _sm.Run(Level);
            _sm.Tick();

            Assert.That(_stepEntered, Is.EqualTo(2));
            Assert.That(_sm.CurrentStep, Is.EqualTo(0));
            Assert.That(_sm.IsRunning, Is.True);
        }

        [Test]
        public void RunNewLevel_WhilePendingDelay_StopsOldTimer()
        {
            SetSteps(Level, DelayedStep());
            SetSteps(Level + 1, DelayedStep());
            _sm.Run(Level);
            _sm.Tick();
            Assert.That(_timer.IsStart, Is.True, "level 1 step delay pending");

            _sm.Run(Level + 1);

            Assert.That(_timer.IsStart, Is.False, "the old delay callback must not fire into the new run");
            Assert.That(_sm.CurrentStepLevel, Is.EqualTo(Level + 1));
            Assert.That(_sm.CurrentStep, Is.EqualTo(0));
        }

        [Test]
        public void BranchNoMatch_PostActionsRunOncePerStep()
        {
            SetSteps(Level, BranchStep(new List<string> { "never" }, new List<int> { 1 }));
            _sm.Run(Level);
            for (int i = 0; i < 5; i++) _sm.Tick();

            Assert.That(_branchPostActions, Is.EqualTo(1));
            Assert.That(_sm.CurrentStep, Is.EqualTo(0));

            _sm.Run(Level);
            _sm.Tick();
            Assert.That(_branchPostActions, Is.EqualTo(2));
        }

        [Test]
        public void BranchConditionsAndAdvanceSteps_DifferentLengths_NoThrow()
        {
            SetSteps(Level, BranchStep(new List<string> { "never", "go" }, new List<int> { 1 }));
            _sm.Run(Level);

            Assert.DoesNotThrow(() => _sm.Tick());
        }

        private static TutorialDataSO.ConditionAction DelayedStep()
        {
            return new TutorialDataSO.ConditionAction
            {
                Condition = "go",
                Action = new TutorialDataSO.ActionData
                {
                    Type = TUTORIAL_ACTION.NONE,
                    Delay = 100f,
                    PostProcessActions = new List<string> { "entered" }
                }
            };
        }

        private static TutorialDataSO.ConditionAction BranchStep(List<string> conditions, List<int> advance)
        {
            return new TutorialDataSO.ConditionAction
            {
                Condition = "go",
                Action = new TutorialDataSO.ActionData
                {
                    Type = TUTORIAL_ACTION.BRANCH,
                    Conditions = conditions,
                    AdvanceSteps = advance,
                    PostProcessActions = new List<string> { "branch-post" }
                }
            };
        }

        private void SetSteps(int level, params TutorialDataSO.ConditionAction[] steps)
        {
            FieldInfo field = typeof(TutorialDataSO).GetField("tutorialDatas", BindingFlags.Instance | BindingFlags.NonPublic);
            var map = (Dictionary<int, TutorialDataSO.TutorialData>)field.GetValue(_data);
            if (map == null)
            {
                map = new Dictionary<int, TutorialDataSO.TutorialData>();
                field.SetValue(_data, map);
            }
            map[level] = new TutorialDataSO.TutorialData { ConditionActions = new List<TutorialDataSO.ConditionAction>(steps) };
        }
    }
}
