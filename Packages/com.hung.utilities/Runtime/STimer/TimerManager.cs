using UnityEngine.Events;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using Hung.DesignPattern;

namespace Hung.Utilities.Timer
{
    [DefaultExecutionOrder(-100)]
    public class TimerManager : Singleton<TimerManager>
    {
        public enum LOOP_TYPE
        {
            UPDATE = 0,
            FIXED_UPDATE = 1
        }
        private const int INIT_STIMER = 50;
        private const int INIT_STIMER_DATA = 50;
        [HideInInspector]
        public event Action TimerUpdate;
        [HideInInspector]
        public event Action TimerFixedUpdate;
        [HideInInspector]
        public event Action TimerLateUpdate;

        private Queue<STimer> sTimers = new Queue<STimer>();
        private List<STimer> inUseSTimers = new List<STimer>();

        private List<STimerData> scaleTimeSTimerDatas = new List<STimerData>();
        private List<STimerData> unScaleTimeSTimerDatas = new List<STimerData>();
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            AddSTimerToPool();
        }
        private void Update()
        {
            TimerUpdate?.Invoke();
        }
        private void FixedUpdate()
        {
            TimerFixedUpdate?.Invoke();
        }
        private void LateUpdate()
        {
            if (scaleTimeSTimerDatas.Count > 0)
            {
                StartWaitForTime(scaleTimeSTimerDatas, false);
            }

            if (unScaleTimeSTimerDatas.Count > 0)
            {
                StartWaitForTime(unScaleTimeSTimerDatas, true);
            }
            TimerLateUpdate?.Invoke();
        }

        private void StartWaitForTime(List<STimerData> stimerData, bool isUnscaleTime = false)
        {
            //NOTE: Scale Timer
            if (stimerData.Count > 1)
            {
                STimer timer = PopSTimer();
                timer.IsUnscaleTime = isUnscaleTime;
                List<Action> actions = new();
                List<float> times = new();
                stimerData.Sort(STimerData.Comparer);

                float lastTime = stimerData[^1].Time;
                for (int i = 0; i < stimerData.Count; i++)
                {
                    actions.Add(stimerData[i].Action);
                    times.Add(stimerData[i].Time);
                }
                actions.Add(() => PushSTimer(timer));
                times.Add(lastTime);

                timer.Start(times, actions);
                stimerData.Clear();
            }
            else if (stimerData.Count == 1)
            {
                STimer timer = PopSTimer();
                timer.IsUnscaleTime = false;
                Action lastAction = stimerData[0].Action;
                Action callBack = () =>
                {
                    lastAction?.Invoke();
                    PushSTimer(timer);
                };
                timer.Start(stimerData[0].Time, callBack);
                stimerData.RemoveAt(0);
            }
        }

        public STimer PopSTimer()
        {
            AddSTimerToPool();
            STimer timer = sTimers.Dequeue();
            inUseSTimers.Add(timer);
            //MGDebug.Log(DevId.Hung, $"STimer-Pool: {sTimers.Count} - STimer-Run: {inUseSTimers.Count}");
            return timer;
        }
        public void PushSTimer(STimer timer, bool checkDuplicated = true) //DEV: Can using Heap to optimize
        {
            if (timer == null) return;
            if (checkDuplicated)
            {
                if (sTimers.Contains(timer))
                {
                    return;
                }
            }

            timer.IsUnscaleTime = false;
            timer.TimeScale = 1f;
            sTimers.Enqueue(timer);
            inUseSTimers.Remove(timer);
        }
        public void WaitForFrame(int frame, Action action) 
        {
            STimer timer = PopSTimer();
            Action timerAction = () =>
            {
                action?.Invoke();
                PushSTimer(timer, false);
            };
            timer.Start(frame, timerAction);
        }
        
        public void WaitForFixedFrame(int frame, Action action)
        {
            STimer timer = PopSTimer();

            timer.Start(frame, TimerAction, STimer.EVENT_TYPE.FRAME_FIXED_UPDATE);
            return;

            void TimerAction()
            {
                action?.Invoke();
                PushSTimer(timer, false);
            }
        }
        
        public void WaitForTime(float time, Action action, bool isUnscaleTime = false) 
        {
            STimerData sTimerData = new(time, action);
            if (!isUnscaleTime)
            {
                scaleTimeSTimerDatas.Add(sTimerData);
            }
            else
            {
                unScaleTimeSTimerDatas.Add(sTimerData);
            }
        }
        public STimer WaitForTime(List<float> times, List<Action> events)
        {
            STimer timer = PopSTimer();
            timer.Start(times, events, () => PushSTimer(timer));
            return timer;
        }
        public void TriggerLoopAction(Action action, float time, int num = 0, bool isUnscaleTime = false)
        {
            if (action == null || num <= 0) return;
            STimer timer = PopSTimer();
            timer.IsUnscaleTime = isUnscaleTime;

            int i = 1;
            Action timerAction = () =>
            {
                if (i >= num)
                {
                    StopTimer(ref timer);
                }
                action?.Invoke();
                i++;
            };

            timer.Start(time, timerAction, true);
        }
        public void StopTimer(ref STimer timer)
        {
            if (timer == null) return;

            timer.Stop();
            PushSTimer(timer);
            timer = null;
        }
        public void TriggerLoopAction(Action action, int frame, LOOP_TYPE type) //NOTE: How it works???
        {
            STimer timer = PopSTimer();
            if (type == LOOP_TYPE.UPDATE)
            {
                timer.ClearEvent(STimer.EVENT_TYPE.FRAME_UPDATE);
                timer.FrameUpdate += action;

                Action timerAction = () => PushSTimer(timer, false);
                timer.Start(frame, timerAction);
            }
            else if(type == LOOP_TYPE.FIXED_UPDATE)
            {
                timer.ClearEvent(STimer.EVENT_TYPE.FRAME_FIXED_UPDATE);
                timer.FrameFixedUpdate += action;
                Action timerAction = () =>
                {
                    PushSTimer(timer, false);
                };
                timer.Start(frame, timerAction, STimer.EVENT_TYPE.FRAME_FIXED_UPDATE);
            }
        }
        public void RecallAllSData()
        {
            for(int i = 0; i < inUseSTimers.Count; i++)
            {
                STimer timer = inUseSTimers[i];
                timer.Stop();
                sTimers.Enqueue(timer);
            }
            inUseSTimers.Clear();
        }
        private void AddSTimerToPool()
        {
            if (sTimers.Count == 0)
            {
                for (int i = 0; i < INIT_STIMER; i++)
                {
                    STimer timer = new STimer();
                    sTimers.Enqueue(timer);
                }
            }
        }
    }

        
}
