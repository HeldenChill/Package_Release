using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Hung.Utilities.Timer
{
    public class STimer 
    {
        public enum EVENT_TYPE
        {
            FRAME_UPDATE = 0,
            FRAME_FIXED_UPDATE = 1
        }

        public enum CAL_TYPE
        {
            SEQUENCE = 0,
            ADD = 1,
        }

        #region Multiple Event Calls
        private List<Action> events;
        private List<float> times;
        private List<float> timeAdds;
        private List<STimerData> sTimerData;
        private float maxTime;
        private int eventIndex = 0;
        #endregion

        #region Event
        private event Action CallBack;
        public event Action FrameUpdate;
        public event Action FrameFixedUpdate;
        public event Action<int> TimeOut;   
        #endregion

        #region Property
        private bool isStart = false;
        private float remainingTime = 0;
        private int timeFrame = 0;
        private int timeFixedFrame = 0;
        private int code = -1;
        private bool isUnscaleTime = false;
        private bool isStopping = false;
        private bool cycleStart = false;

        private bool isLoop = false;
        private float loopTime = 0;
        private int loopFrame = 0;
        public float RemainingTime => remainingTime;
        public bool IsStart => isStart;
        public bool IsUnscaleTime
        {
            get => isUnscaleTime;
            set => isUnscaleTime = value;
        }
        public float TimeScale = 1f;
        #endregion
        // Start is called before the first frame update

        private bool isStartFrame = false;
        public STimer()
        {
            TimerManager.Ins.TimerUpdate += Update;
            TimerManager.Ins.TimerFixedUpdate += FixedUpdate;
            TimerManager.Ins.TimerLateUpdate += LateUpdate;
        }

        public void Start(float time, int code = -1)
        {
            this.code = code;
            Start(time);
        }
        public void Start(int frame, int code = -1, EVENT_TYPE type = EVENT_TYPE.FRAME_UPDATE)
        {
            this.code = code;
            Start(frame, type);
        }
        public void Start(int frame, Action action, EVENT_TYPE type = EVENT_TYPE.FRAME_UPDATE)
        {
            this.CallBack = action;
            Start(frame, type);
        }
        public void Start(float time, Action action, bool isLoop = false)
        {
            this.isLoop = isLoop;
            loopTime = time;
            this.CallBack = action;
            Start(time);
        }
        public void Start(float time)
        {
            if (isStopping)
            {
                Debug.LogWarning("Start Timer call in callback of itself, Then it may not start properly!");
                cycleStart = true;
            }
            if(time > 0)
            {
                isStart = true;
                remainingTime = time;
            }
            else
            {
                TriggerEvent();
                Stop();
            }
            isStartFrame = true;
        }
        public void Start(int frame, EVENT_TYPE type)
        {           
            if(type == EVENT_TYPE.FRAME_UPDATE)
            {
                if (frame > 0)
                {
                    isStart = true;
                    timeFrame = frame + 1;
                    FrameUpdate?.Invoke();
                }
                else
                {
                    TriggerEvent();
                    Stop();
                }
            }
            else if(type == EVENT_TYPE.FRAME_FIXED_UPDATE)
            {
                if (frame > 0)
                {
                    isStart = true;
                    timeFixedFrame = frame + 1;
                    FrameFixedUpdate?.Invoke();
                }
                else
                {
                    TriggerEvent();
                    Stop();
                }
            }
            isStartFrame = true;
        }
        public void Start(List<float> times, List<Action> events, Action callBack = null,CAL_TYPE type = CAL_TYPE.SEQUENCE)
        {
            //NOTE:Reference new data
            this.times = times;
            this.events = events;

            if(type == CAL_TYPE.ADD)
            {
                float time = 0;
                if(timeAdds == null)
                {
                    timeAdds = new List<float>();
                }
                timeAdds.Clear();
                for (int i = 0; i < times.Count; i++)
                {
                    time += times[i];
                    timeAdds.Add(time);
                }
                this.times = timeAdds;
            }

            //NOTE: Starting STimer
            isStart = false;
            CallBack = callBack;
            maxTime = this.times[this.times.Count - 1];
            eventIndex = 0;

            //CASE: One time and one event or all events happens in 0
            if(maxTime > 0)
            {
                Start(maxTime);
            }
            else
            {
                for(int i = 0; i < events.Count; i++)
                {
                    events[i]?.Invoke();
                }
            }
            
        }
        public void Start(List<STimerData> sTimerData, Action callBack = null)
        {
            this.sTimerData = sTimerData;

            isStart = false;
            CallBack = callBack;
            maxTime = sTimerData[sTimerData.Count - 1].Time;
            eventIndex = 0;

            if(maxTime > 0)
            {
                Start(maxTime);
            }
            else
            {
                for(int i = 0; i < sTimerData.Count; i++)
                {
                    sTimerData[i].Action?.Invoke();
                }
            }
        }
        public void Stop()
        {
            if (cycleStart)
            {
                cycleStart = false;
                return;
            }
            isStart = false;
            events = null;
            times = null;
            sTimerData = null;
            remainingTime = 0;
            timeFrame = 0;
            CallBack = null;
            isLoop = false;
        }
        private void Update()
        {
            if (isStart)
            {
                CounterUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (isStart)
            {
                CounterFixedUpdate();
            }
        }
        private void LateUpdate()
        {
            isStartFrame = false;
        }
        private void CounterUpdate()
        {
            if (remainingTime > 0)
            {
                if(!isUnscaleTime)
                    remainingTime -= Time.deltaTime * TimeScale;
                else
                    remainingTime -= Time.unscaledDeltaTime * TimeScale;

                if(times != null && events != null)
                {
                    int maxEventIndex = events.Count;
                    for(; eventIndex < maxEventIndex;)
                    {
                        if (maxTime - remainingTime >= times[eventIndex])
                        {
                            eventIndex += 1;
                            events[eventIndex - 1].Invoke();               
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                }
                if (sTimerData != null)
                {
                    int maxEventIndex = sTimerData.Count;
                    for (; eventIndex < maxEventIndex;)
                    {
                        if (maxTime - remainingTime >= sTimerData[eventIndex].Time)
                        {
                            eventIndex += 1;
                            sTimerData[eventIndex - 1].Action.Invoke();
                        }
                        else
                        {
                            break;
                        }
                    }

                }

                if (remainingTime <= 0)
                {        
                    isStopping = true;            
                    TriggerEvent();
                    if (!isLoop)
                        Stop();
                    else
                        remainingTime = loopTime;
                    isStopping = false;
                }
            }

            if (timeFrame > 0)
            {
                isStopping = true;
                timeFrame -= 1;
                if (timeFrame <= 0)
                {
                    TriggerEvent();
                    if (!isLoop)
                        Stop();
                    else
                        timeFrame = loopFrame;
                }
                isStopping = false;
            }
            if(!isStartFrame)
                FrameUpdate?.Invoke();
        }
        private void CounterFixedUpdate()
        {
            if (timeFixedFrame > 0)
            {
                timeFixedFrame -= 1;
                if (timeFixedFrame <= 0)
                {         
                    isStopping = true;         
                    TriggerEvent();
                    if (!isLoop)
                        Stop();
                    else
                        timeFixedFrame = loopFrame;
                    isStopping = false;
                }
            }
            if (!isStartFrame)
                FrameFixedUpdate?.Invoke();
        }
        private void TriggerEvent()
        {
            TimeOut?.Invoke(code);
            CallBack?.Invoke();
        }

        public void ClearEvent(EVENT_TYPE type)
        {
            switch (type)
            {
                case EVENT_TYPE.FRAME_UPDATE:
                    FrameUpdate = null;
                    break;
                case EVENT_TYPE.FRAME_FIXED_UPDATE:
                    FrameFixedUpdate = null;
                    break;
            }
        }
        ~STimer()
        {
            TimerManager.Ins.TimerUpdate -= Update;
            TimerManager.Ins.TimerFixedUpdate -= FixedUpdate;
            TimerManager.Ins.TimerLateUpdate -= LateUpdate;
        }
        
    }
    public class STimerData
    {
        public float Time;
        public Action Action;
        public static STimerDataComparer Comparer = new STimerDataComparer();

        public STimerData()
        {

        }
        public STimerData(float time, Action action)
        {
            this.Time = time;
            this.Action = action;
        }
        public void SetData(float time, Action action)
        {
            Time = time;
            Action = action;
        }
    }
    public class STimerDataComparer : IComparer<STimerData>
    {
        public int Compare(STimerData x, STimerData y)
        {
            if(x.Action == null && y.Action != null)
            {
                return 1;
            }
            else if(x.Action != null && y.Action == null)
            {
                return -1;
            }
            else
            {
                return x.Time.CompareTo(y.Time);

            }
        }
    }
}
