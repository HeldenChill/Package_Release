namespace Hung.Base
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    public enum AREA_TYPE
    {
        DANGER,
        SAFE,
        BUFF,
        DEBUFF,
        NEUTRAL
    }
    
    [Serializable]
    public class AreaData
    {
        public List<Cube> Cubes = new List<Cube>();
        public AREA_TYPE Type;
        public float Intensity;
        public float Duration;
        public float CreationTime;
        public object Source;
        
        public bool IsActive => Time.time < CreationTime + Duration;
        
        public AreaData(AREA_TYPE type, float intensity, float duration, object source)
        {
            Type = type;
            Intensity = intensity;
            Duration = duration;
            CreationTime = Time.time;
            Source = source;
        }
    }
}
