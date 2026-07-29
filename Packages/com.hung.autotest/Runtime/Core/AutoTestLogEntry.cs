using System;
using UnityEngine;

namespace Hung.AutoTest
{
    [Serializable]
    public sealed class AutoTestLogEntry
    {
        public string condition;
        public string stackTrace;
        public LogType type;
        public float realtime;
        public float elapsed;
        public int frame;
    }
}
