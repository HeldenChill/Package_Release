using System;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Hung.Diagnostics
{
    public static class GameLog
    {
        private static GameLogConfig config;
        private static JsonFileLogSink fileSink;

        private static string sessionId;
        private static long sequence;

        [ThreadStatic]
        private static string currentFlowId;

        private static readonly JsonSerializerSettings jsonSettings = new()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static bool initialized;
        private static bool isInternalConsoleEcho;
        private static int mainThreadId;

        public static string SessionId => sessionId;
        public static string CurrentLogFilePath => fileSink?.CurrentFilePath;

        public static void Initialize(GameLogConfig logConfig)
        {
            if (initialized)
                return;

            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            config = logConfig;

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameLogConfig>();
            }

            sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" +
                        Guid.NewGuid().ToString("N").Substring(0, 6);

            if (config.enableFileLog)
            {
                string logDir = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    "GameLogs"
                );

                fileSink = new JsonFileLogSink(
                    logDir,
                    sessionId,
                    config.flushIntervalMs,
                    config.maxFileSizeMb
                );
            }

            if (config.captureUnityDebugLog)
            {
                Application.logMessageReceivedThreaded += OnUnityLogReceived;
            }

            initialized = true;

            Info("Diagnostics", "Session.Start", "Game diagnostics session started", new
            {
                sessionId,
                appVersion = Application.version,
                platform = Application.platform.ToString(),
                unityVersion = Application.unityVersion,
                persistentDataPath = Application.persistentDataPath
            });
        }

        public static void Shutdown()
        {
            if (!initialized)
                return;

            Info("Diagnostics", "Session.End", "Game diagnostics session ended");

            Application.logMessageReceivedThreaded -= OnUnityLogReceived;

            fileSink?.Dispose();
            fileSink = null;

            initialized = false;
        }

        public static void Trace(string area, string eventName, string message = null, object data = null)
            => Write(GameLogLevel.Trace, area, eventName, message, data);

        public static void DebugLog(string area, string eventName, string message = null, object data = null)
            => Write(GameLogLevel.Debug, area, eventName, message, data);

        public static void Info(string area, string eventName, string message = null, object data = null)
            => Write(GameLogLevel.Info, area, eventName, message, data);

        public static void Warning(string area, string eventName, string message = null, object data = null)
            => Write(GameLogLevel.Warning, area, eventName, message, data);

        public static void Error(string area, string eventName, string message = null, object data = null, Exception exception = null)
            => Write(GameLogLevel.Error, area, eventName, message, data, exception);

        public static void Fatal(string area, string eventName, string message = null, object data = null, Exception exception = null)
            => Write(GameLogLevel.Fatal, area, eventName, message, data, exception);

        public static string BeginFlow(string area, string eventName, object data = null)
        {
            string previous = currentFlowId;
            string newFlowId = CreateFlowId(eventName);

            currentFlowId = newFlowId;

            Write(GameLogLevel.Info, area, eventName + ".Begin", "Flow started", new
            {
                flowId = newFlowId,
                parentFlowId = previous,
                data
            });

            return newFlowId;
        }

        public static void EndFlow(string flowId)
        {
            Write(GameLogLevel.Info, "Flow", "Flow.End", "Flow ended", new
            {
                flowId
            });

            if (currentFlowId == flowId)
            {
                currentFlowId = null;
            }
        }

        public static GameLogScope Scope(string area, string eventName, object data = null)
        {
            string previous = currentFlowId;
            string flowId = BeginFlow(area, eventName, data);
            return new GameLogScope(previous, flowId, shouldEndFlow: true);
        }

        internal static void SetCurrentFlow(string flowId)
        {
            currentFlowId = flowId;
        }

        private static void Write(
            GameLogLevel level,
            string area,
            string eventName,
            string message = null,
            object data = null,
            Exception exception = null)
        {
            if (!initialized)
                return;

            if (config != null && level < config.minimumLevel)
                return;

            long seq = Interlocked.Increment(ref sequence);
            bool onMainThread = Thread.CurrentThread.ManagedThreadId == mainThreadId;

            var record = new GameLogRecord
            {
                timeUtc = DateTime.UtcNow.ToString("o"),
                // ponytail: Time.* is main-thread-only; logMessageReceivedThreaded can fire off-thread during scene load
                realtime = onMainThread ? Time.realtimeSinceStartup : -1f,
                frame = onMainThread ? Time.frameCount : -1,
                sequence = seq,

                sessionId = sessionId,
                flowId = currentFlowId,

                level = level.ToString(),
                area = area,
                eventName = eventName,
                message = message,

                data = data,

                scene = GetActiveSceneName(),
                exception = exception?.ToString(),
                stackTrace = ShouldIncludeStackTrace(level)
                    ? new StackTrace(2, true).ToString()
                    : null
            };

            string jsonLine;

            try
            {
                jsonLine = JsonConvert.SerializeObject(record, jsonSettings);
            }
            catch (Exception serializeException)
            {
                record.data = new
                {
                    serializationFailed = true,
                    originalDataType = data?.GetType().FullName,
                    error = serializeException.Message
                };

                jsonLine = JsonConvert.SerializeObject(record, jsonSettings);
            }

            fileSink?.Enqueue(jsonLine);

            if (config != null && config.echoToUnityConsole)
            {
                EchoToUnityConsole(level, area, eventName, message);
            }
        }

        private static bool ShouldIncludeStackTrace(GameLogLevel level)
        {
            if (config == null)
                return false;

            if (level >= GameLogLevel.Error)
                return config.includeStackTraceForError;

            if (level == GameLogLevel.Warning)
                return config.includeStackTraceForWarning;

            return false;
        }

        private static string GetActiveSceneName()
        {
            try
            {
                return SceneManager.GetActiveScene().name;
            }
            catch
            {
                return null;
            }
        }

        private static string CreateFlowId(string eventName)
        {
            string cleanName = string.IsNullOrWhiteSpace(eventName)
                ? "flow"
                : eventName.Replace(".", "_").Replace(" ", "_").ToLowerInvariant();

            return $"{cleanName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        private static void EchoToUnityConsole(
            GameLogLevel level,
            string area,
            string eventName,
            string message)
        {
            if (isInternalConsoleEcho)
                return;

            try
            {
                isInternalConsoleEcho = true;

                string text = $"[{area}] {eventName} - {message}";

                if (level >= GameLogLevel.Error)
                    Debug.LogError(text);
                else if (level == GameLogLevel.Warning)
                    Debug.LogWarning(text);
                else
                    Debug.Log(text);
            }
            finally
            {
                isInternalConsoleEcho = false;
            }
        }

        private static void OnUnityLogReceived(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (isInternalConsoleEcho)
                return;

            GameLogLevel level = type switch
            {
                LogType.Error => GameLogLevel.Error,
                LogType.Assert => GameLogLevel.Error,
                LogType.Exception => GameLogLevel.Fatal,
                LogType.Warning => GameLogLevel.Warning,
                _ => GameLogLevel.Debug
            };

            Write(level, "Unity", "Unity.DebugLog", condition, new
            {
                unityLogType = type.ToString()
            });
        }
    }
}