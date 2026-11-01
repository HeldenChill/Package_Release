using System;
using Hung.Base;
using Hung.UI;
using Hung.DesignPattern;
using UnityEngine;
using UnityEngine.UI;
using Hung.Utilities.Timer;

namespace Hung.Tutorial
{
    public class TutorialManager : Singleton<TutorialManager>, ITutorialService
    {
        [SerializeField] protected TutorialDataSO data;
        [SerializeField] protected bool isActive = true;

        private STimer _timer;
        private TutorialRegistry _registry;
        private TutorialStepContext _ctx;
        private TutorialUIController _ui;
        private TutorialCoordinateResolver _resolver;
        private TutorialStateMachine _sm;

        public Func<int, HandData> GetHandInfo { get; set; }
        public int CurrentStep => _sm != null ? _sm.CurrentStep : -1;
        public int CurrentStepLevel => _sm != null ? _sm.CurrentStepLevel : -1;
        public bool IsRunning => _sm != null && _sm.IsRunning;

        protected void Awake()
        {
            if (!Hung.Base.ServiceSlotLifecycle.CanInitialize<ITutorialService>(this, this)) return;

            DontDestroyOnLoad(gameObject);
            ServiceSlots.Register<ITutorialService>(this);
            _timer = TimerManager.Ins.PopSTimer();

            _registry = new TutorialRegistry();
            _ctx = new TutorialStepContext();
            _ui = new TutorialUIController();
            _resolver = new TutorialCoordinateResolver(
                (world, rect) => UIScreenUtils.WorldToUI(Locator.UI.Canvas, world, rect));
            _sm = new TutorialStateMachine(_registry, _ui, _resolver, _ctx);
            _sm.GetHandInfo = step => GetHandInfo?.Invoke(step);
            _sm.StartShowVideo = desc => StartCoroutine(_ui.ShowVideo(desc));
            _sm.Configure(data, _timer);

            RegisterBuiltInActions();
        }

        protected void OnDestroy()
        {
            ServiceSlots.Unregister<ITutorialService>(this);

            if (_timer != null) TimerManager.Ins?.PushSTimer(_timer);
        }

        private void RegisterBuiltInActions()
        {
            // _registry.AddAction(TutorialKeys.EnableInput, () => SInput.Ins.Enable());
            // _registry.AddAction(TutorialKeys.DisableInput, () => SInput.Ins.Disable());
            _registry.AddAction(TutorialKeys.UpdateUI, () => { });
            _registry.AddAction(TutorialKeys.GetHandInfo, () =>
            {
                if (GetHandInfo == null) return;
                HandData hd = GetHandInfo.Invoke(_sm.CurrentStep);
                _resolver.ResolveHand(_ctx, _ui.CanvasRectTf, hd.Pos1, hd.Pos2, hd.PosType);
                WriteCtxToCurrentAction();
            });
            _registry.AddAction(TutorialKeys.GetOldHandUIPos, () =>
            {
                _resolver.ResolveHand(_ctx, _ui.CanvasRectTf, _ctx.OldPosition, default, POSITION_TYPE.UI_POS);
                WriteCtxToCurrentAction();
            });
            _registry.AddAction(TutorialKeys.GetOldHandWorldPos, () =>
            {
                _resolver.ResolveHand(_ctx, _ui.CanvasRectTf, _ctx.OldPosition, default, POSITION_TYPE.WORLD_POS);
                WriteCtxToCurrentAction();
            });
        }

        private void WriteCtxToCurrentAction()
        {
            if (!data.TutorialDatas.ContainsKey(_sm.CurrentStepLevel)) return;
            var ca = data.TutorialDatas[_sm.CurrentStepLevel].ConditionActions;
            if (_sm.CurrentStep < ca.Count)
            {
                ca[_sm.CurrentStep].Action.Pos1 = _ctx.Pos1;
                ca[_sm.CurrentStep].Action.Pos2 = _ctx.Pos2;
            }
        }

        public void Init()
        {
            _ui.Acquire();
        }

        public bool IsHaveTutorial(int level)
        {
            if (!isActive) return false;
            return _sm.IsHaveTutorial(level);
        }

        public void Run(int level)
        {
            if (!isActive) return;
            _sm.Run(level);
        }

        public void Stop() => _sm.Stop();

        public void AddCustomAction(string key, Action action) => _registry.AddAction(key, action);
        public void RemoveCustomAction(string key) => _registry.RemoveAction(key);
        public void AddCustomCondition(string key, Func<bool> func) => _registry.AddCondition(key, func);
        public void RemoveCustomCondition(string key) => _registry.RemoveCondition(key);

        public void SetCalloutTarget(Transform rect, Image frame, Vector3 worldPos, POSITION_TYPE posType)
        {
            _ctx.CallOutRectTf = rect;
            _ctx.CallOutFrame = frame;
            _ctx.IsUI = true;
            _resolver.ResolveHand(_ctx, _ui.CanvasRectTf, worldPos, default, posType);
            if (data.TutorialDatas.ContainsKey(_sm.CurrentStepLevel))
            {
                var ca = data.TutorialDatas[_sm.CurrentStepLevel].ConditionActions;
                if (_sm.CurrentStep < ca.Count)
                {
                    var a = ca[_sm.CurrentStep].Action;
                    a.CallOutRectTf = rect;
                    a.CallOutFrame = frame;
                    a.IsUI = true;
                    a.Pos1 = _ctx.Pos1;
                    a.Pos2 = _ctx.Pos2;
                }
            }
        }

        protected void Update() => _sm.Tick();
    }
}
