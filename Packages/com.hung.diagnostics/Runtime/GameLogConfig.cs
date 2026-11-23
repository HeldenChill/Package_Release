using UnityEngine;

namespace Hung.Diagnostics
{
    public enum GameLogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }
    [CreateAssetMenu(
        fileName = "GameLogConfig",
        menuName = "Game Diagnostics/Game Log Config"
    )]
    public sealed class GameLogConfig : ScriptableObject
    {
        public bool enableFileLog = true;
        public bool captureUnityDebugLog = true;
        public bool includeStackTraceForWarning = false;
        public bool includeStackTraceForError = true;

        public GameLogLevel minimumLevel = GameLogLevel.Info;

        public int maxQueueSize = 5000;
        public int flushIntervalMs = 500;
        public int maxFileSizeMb = 10;
        public int maxLogFiles = 5;

        public bool echoToUnityConsole = false;
    }
}
