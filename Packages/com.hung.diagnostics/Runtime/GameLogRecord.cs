using UnityEngine;

namespace Hung.Diagnostics
{
    public class GameLogRecord
    {
        public string timeUtc;
        public float realtime;
        public int frame;
        public long sequence;

        public string sessionId;
        public string flowId;
        public string parentFlowId;

        public string level;
        public string area;
        public string eventName;
        public string message;

        public object data;

        public string scene;
        public string stackTrace;
        public string exception;
    }
}
