using System.Collections.Generic;
using UnityEngine;

namespace Hung.AutoTest
{
    public sealed class AutoTestLogCollector
    {
        private readonly List<AutoTestLogEntry> entries = new List<AutoTestLogEntry>();
        private AutoTestContext context;
        private bool isListening;

        public IReadOnlyList<AutoTestLogEntry> Entries
        {
            get { return entries; }
        }

        public void Start(AutoTestContext autoTestContext)
        {
            context = autoTestContext;
            entries.Clear();

            if (isListening)
                return;

            Application.logMessageReceived += OnLogReceived;
            isListening = true;
        }

        public void Stop()
        {
            if (!isListening)
                return;

            Application.logMessageReceived -= OnLogReceived;
            isListening = false;
            context = null;
        }

        public void Clear()
        {
            entries.Clear();
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            AutoTestLogEntry entry = new AutoTestLogEntry
            {
                condition = condition,
                stackTrace = stackTrace,
                type = type,
                realtime = Time.realtimeSinceStartup,
                elapsed = context != null ? context.ElapsedSeconds : 0f,
                frame = Time.frameCount
            };

            entries.Add(entry);

            if (context != null)
                context.Logs.Add(entry);
        }
    }
}
