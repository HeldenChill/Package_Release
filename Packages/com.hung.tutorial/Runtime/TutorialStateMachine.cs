using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hung.Base;
using UnityEngine;
using Utilities;
using Hung.Utilities.Timer;

namespace Hung.Tutorial
{
    public class TutorialStateMachine
    {
        private readonly TutorialRegistry _registry;
        private readonly TutorialUIController _ui;
        private readonly TutorialCoordinateResolver _resolver;
        private readonly TutorialStepContext _ctx;

        private TutorialDataSO _data;
        private STimer _timer;
        private TutorialDataSO.TutorialData _tutorialData;
        private int _level;
        private int _currentStep;
        private bool _isRunning;
        private bool _isCheckedCondition;

        public int CurrentStep => _currentStep;
        public int CurrentStepLevel => _level;
        public bool IsRunning => _isRunning;
        public Func<int, HandData> GetHandInfo { get; set; }

        // Injected by TutorialManager (MonoBehaviour) — plain class cannot call StartCoroutine
        public Action<string> StartShowVideo = _ => { };

        public TutorialStateMachine(TutorialRegistry registry, TutorialUIController ui,
            TutorialCoordinateResolver resolver, TutorialStepContext ctx)
        {
            _registry = registry;
            _ui = ui;
            _resolver = resolver;
            _ctx = ctx;
        }

        public void Configure(TutorialDataSO data, STimer timer)
        {
            _data = data;
            _timer = timer;
        }

        public bool IsHaveTutorial(int level)
        {
            _level = level;
            if (_data.TutorialDatas.ContainsKey(level))
            {
                _tutorialData = _data.TutorialDatas[level];
                return true;
            }
            _tutorialData = null;
            return false;
        }

        public void Run(int level)
        {
            if (!IsHaveTutorial(level)) return;
            _currentStep = 0;
            _isRunning = true;
            _ctx.Reset();
        }

        public void Stop()
        {
            _tutorialData = null;
            _isRunning = false;
            _timer.Stop();
        }

        public void Tick()
        {
            if (!_isRunning) return;
            if (_tutorialData?.ConditionActions == null) return;
            if (_isCheckedCondition) return;
            if (_currentStep < _tutorialData.ConditionActions.Count)
            {
                string id = _tutorialData.ConditionActions[_currentStep].Condition;
                bool value = _registry.EvaluateCondition(id);
                // Debug.Log($"ID={id} - value={value}");
                if (value)
                {
                    NextStep();
                }
            }
        }

        public Task PrepareTutorial(int level, int step = -1)
        {
            Task task = null;
            if (_data.TutorialDatas.ContainsKey(level))
            {
                _tutorialData = _data.TutorialDatas[level];
            }
            // LoadingDatas is optional per level — Odin leaves the dictionary null when the author never
            // fills that section in, so it must be null-checked, not just _tutorialData.
            if (_tutorialData?.LoadingDatas == null
                || !_tutorialData.LoadingDatas.ContainsKey(step)
                || _tutorialData.LoadingDatas[step].Count == 0)
            {
                return null;
            }
            for (int i = 0; i < _tutorialData.LoadingDatas[step].Count; i++)
            {
                var loading = _tutorialData.LoadingDatas[step][i];
                switch (loading.Type)
                {
                    case TUTORIAL_LOADING.PREPARE_VIDEO:
                        if (!string.IsNullOrEmpty(loading.VideoName))
                        {
                            DevLog.Log(DevId.System, $"Prepare video - Stream: {loading.VideoName}");
                            task = _ui.PrepareVideo(loading.VideoName);
                        }
                        else
                        {
                            DevLog.Log(DevId.System, $"Prepare video - Video: {loading.Video.name}");
                            task = _ui.PrepareVideo(loading.Video);
                        }
                        break;
                }
            }
            return task;
        }

        private void NextStep()
        {
            if (_tutorialData.ConditionActions.Count <= _currentStep)
            {
                _ui.CloseAll();
                _isRunning = false;
                return;
            }

            PrepareTutorial(_level, _currentStep);
            var action = _tutorialData.ConditionActions[_currentStep].Action;

            switch (action.Type)
            {
                case TUTORIAL_ACTION.END_TUTORIAL:
                    RunActions(action.PostProcessActions);
                    _ui.CloseAll();
                    _isRunning = false;
                    return;
                case TUTORIAL_ACTION.BRANCH:
                    RunActions(action.PostProcessActions);
                    for (int i = 0; i < action.Conditions.Count; i++)
                    {
                        if (_registry.EvaluateCondition(action.Conditions[i]))
                        {
                            _currentStep += action.AdvanceSteps[i];
                            return;
                        }
                    }
                    return;
            }

            _isCheckedCondition = true;
            RunActions(action.PostProcessActions);

            _timer.Start(action.Delay, () =>
            {
                switch (action.Type)
                {
                    case TUTORIAL_ACTION.CLOSE_ALL:
                        _ui.CloseAll();
                        break;
                    case TUTORIAL_ACTION.NONE:
                        break;
                    case TUTORIAL_ACTION.HAND:
                        _ui.PlayHand(action);
                        break;
                    case TUTORIAL_ACTION.SHOW_VIDEO:
                        StartShowVideo(action.Description);
                        break;
                    case TUTORIAL_ACTION.SHOW_CONTENT:
                        _ui.ShowContentItem(action.ContentItemId);
                        break;
                    case TUTORIAL_ACTION.SHOW_CONTENT_STRING:
                        _ui.ShowContentString(action.ContentString);
                        break;
                    case TUTORIAL_ACTION.CALL_OUT:
                        _ui.ShowCallOut(action);
                        break;
                    case TUTORIAL_ACTION.END_TUTORIAL:
                        break;
                }
                _currentStep += 1;
                _isCheckedCondition = false;
                RunActions(action.OtherActions);
                if (_currentStep >= _tutorialData.ConditionActions.Count)
                {
                    _ui.CloseAll();
                    _isRunning = false;
                }
            });
        }

        private void RunActions(List<string> keys)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                _registry.InvokeAction(keys[i]);
            }
        }
    }
}
