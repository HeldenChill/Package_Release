using System;
using UnityEngine;
using Utilities;
namespace SStats
{
    [Serializable]
    public class TrackingStat
    {
        public event Action<TrackingStat, float, float> _OnValueChange;
        public event Action<TrackingStat, float> _OnDownMax;
        public event Action<TrackingStat, float> _OnUpMax;
        public event Action<TrackingStat, float> _OnDownMin;
        public event Action<TrackingStat, float> _OnUpMin;

        protected Stat max;
        protected Stat min;
        protected float _value;
        public float Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    float newValue = _value;
                    HandleBoundaryValue(ref newValue);
                    HandleActions(ref newValue);
                    _value = newValue;
                }
                void HandleBoundaryValue(ref float newValue)
                {
                    if (value > max.Value)
                    {
                        newValue = max.Value;
                    }
                    else if (value < min.Value)
                    {
                        newValue = min.Value;
                    }
                    else
                    {
                        newValue = value;
                    }
                }
                void HandleActions(ref float newValue)
                {
                    if (Mathf.Abs(newValue - _value) > 0.00001f)
                    {
                        if (Mathf.Abs(max.Value - _value) < 0.00001f)
                        {
                            _OnDownMax?.Invoke(this, newValue);
                        }
                        if (Mathf.Abs(max.Value - newValue) < 0.00001f)
                        {
                            _OnUpMax?.Invoke(this, newValue);
                        }
                        if (Mathf.Abs(min.Value - _value) < 0.00001f)
                        {
                            _OnUpMin?.Invoke(this, newValue);
                        }
                        if (Mathf.Abs(min.Value - newValue) < 0.00001f)
                        {
                            _OnDownMin?.Invoke(this, newValue);
                        }
                        _OnValueChange?.Invoke(this, newValue, newValue - _value);
                    }
                }
            }
        }
        public Stat Max => max;
        public Stat Min => min;
        public TrackingStat(Stat max, Stat min)
        {
            this.max = max;
            this.min = min;
            _value = max.Value;
            _OnValueChange?.Invoke(this, _value, 0);
        }
        public void Reset()
        {
            _value = max.Value;
        }
    }
}