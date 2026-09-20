using System.Collections.Generic;
using Gameplay.Character;
using SStats;
using UnityEngine;
using Utilities;
using Hung.Utilities.Timer;

public class RegenStatModule : MonoBehaviour
{
    protected List<TrackingStat> trackingStats;
    protected List<Stat> regenStats;
    protected List<Stat> timeRegenStats;
    protected List<STimer> regenTimers;
    protected List<float> stepValues;
    protected List<float> registerValues = new List<float>();
    protected bool isInit = false;
    public RegenStatModule WithStats(List<TrackingStat> trackingStats)
    {
        UnregisterTrackingValue(trackingStats);
        this.trackingStats = trackingStats;
        RegisterTrackingValue(trackingStats);
        return this;
    }
    public RegenStatModule WithRegenValue(List<Stat> regenStats)
    {
        this.regenStats = regenStats;
        return this;
    }
    public RegenStatModule WithRegenTime(List<Stat> timeRegenStats)
    {
        this.timeRegenStats = timeRegenStats;
        regenTimers = new List<STimer>();
        for (int i = 0; i < timeRegenStats.Count; i++)
        {
            if (timeRegenStats[i] != null)
            {
                regenTimers.Add(TimerManager.Ins.PopSTimer());
            }
            else
            {
                regenTimers.Add(null);
            }
        }
        return this;
    }
    public RegenStatModule WithStepValue(List<float> stepValues)
    {
        this.stepValues = stepValues;
        return this;
    }
    
    protected void OnTrackingValueChange(TrackingStat stat, float value, float valueChange)
    {
        int index = trackingStats.FindIndex(x => x == stat);
        if (index >= 0)
        {
            float checkValue = value - registerValues[index];
            if (Mathf.Abs(checkValue) > stepValues[index])
            {
                registerValues[index] = value;
                if (checkValue < 0)
                {
                    regenTimers[index].Stop();
                    regenTimers[index].Start(timeRegenStats[index].Value);
                }
            }
        }
    }
    protected void OnTrackingValueDownMin(TrackingStat stat, float value)
    {
        
    }
    protected void OnTrackingValueUpMin(TrackingStat stat, float value)
    {
        
    }
    protected void RegisterTrackingValue(List<TrackingStat> trackingStats)
    {
        if (trackingStats == null) return;
        registerValues.Clear();
        for (int i = 0; i < trackingStats.Count; i++)
        {
            trackingStats[i]._OnValueChange += OnTrackingValueChange;
            trackingStats[i]._OnDownMin += OnTrackingValueDownMin;
            trackingStats[i]._OnUpMin += OnTrackingValueUpMin;
            registerValues.Add(trackingStats[i].Value);
        }
    }
    protected void UnregisterTrackingValue(List<TrackingStat> trackingStats)
    {
        if (trackingStats == null) return;
        for (int i = 0; i < trackingStats.Count; i++)
        {
            trackingStats[i]._OnValueChange -= OnTrackingValueChange;
            trackingStats[i]._OnDownMin -= OnTrackingValueDownMin;     
            trackingStats[i]._OnUpMin -= OnTrackingValueUpMin;
        }
    }
    protected void Update()
    {
        if (regenStats != null)
            for (int i = 0; i < regenStats.Count; i++)
            {
                if (regenTimers[i] == null) continue;
                // DevLog.Log(DevId.System, $"TIMER - {regenTimers[i].RemainingTime}");

                if (!regenTimers[i].IsStart && (trackingStats[i].Value < trackingStats[i].Max.Value))
                {
                    trackingStats[i].Value += regenStats[i].Value * Time.deltaTime;
                    // DevLog.Log(DevId.System, $"REGEN - {regenStats[i].Value * Time.deltaTime} => {trackingStats[i].Value}");
                }
            }
    }
}
