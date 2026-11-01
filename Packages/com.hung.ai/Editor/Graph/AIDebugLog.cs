using System.Collections.Generic;
namespace Hung.AI.Editor
{
    public enum AIDebugLogKind
    {
        Info,
        Run,
        Success,
        Failure,
        Abort
    }

    public readonly struct AIDebugLogEntry
    {
        public readonly int Tick;
        public readonly string Text;
        public readonly AIDebugLogKind Kind;

        public AIDebugLogEntry(int tick, string text, AIDebugLogKind kind)
        {
            Tick = tick;
            Text = text;
            Kind = kind;
        }
    }

    /// <summary>
    /// Console lines for the debugger. Logs only status transitions (a node starts, finishes after running, or is aborted),
    /// because NodeTicked fires for every running node on every tick. Keeps the last Capacity lines.
    /// </summary>
    public sealed class AIDebugLog
    {
        public const int Capacity = 200;
        readonly List<AIDebugLogEntry> entries = new List<AIDebugLogEntry>();
        readonly Dictionary<string, NodeStatus> previous = new Dictionary<string, NodeStatus>();
        int tick;
        int lastFrame = -1;
        int abortEntryIndex = -1;
        int abortFrame = -1;

        /// <summary>Oldest first. Read-only for callers; a ListView binds to it directly.</summary>
        public List<AIDebugLogEntry> Entries => entries;

        /// <summary>Number of distinct frames in which a node ticked since the last Clear.</summary>
        public int Tick => tick;

        /// <summary>Changes whenever Entries changes, so a view can skip redundant refreshes.</summary>
        public int Version { get; private set; }

        public void Clear()
        {
            entries.Clear();
            previous.Clear();
            tick = 0;
            lastFrame = -1;
            abortEntryIndex = -1;
            abortFrame = -1;
            Version++;
        }

        public void Info(string text)
        {
            Add(text, AIDebugLogKind.Info);
        }

        /// <summary>Feed every NodeTicked call. Titles should be cached by the caller so non-transition calls allocate nothing.</summary>
        public void Record(string nodeId, string title, string parentTitle, NodeStatus status, int frame)
        {
            if (frame != lastFrame)
            {
                lastFrame = frame;
                tick++;
            }
            previous.TryGetValue(nodeId, out NodeStatus before);
            previous[nodeId] = status;
            bool wasRunning = before == NodeStatus.Running;
            bool hasParent = !string.IsNullOrEmpty(parentTitle);
            switch (status)
            {
                case NodeStatus.Running:
                    if (wasRunning) return;
                    Add((hasParent ? parentTitle + ": " : string.Empty) + title + ".OnStart", AIDebugLogKind.Run);
                    if (abortFrame == frame && abortEntryIndex >= 0 && abortEntryIndex < entries.Count - 1)
                    {
                        AIDebugLogEntry aborted = entries[abortEntryIndex];
                        entries[abortEntryIndex] = new AIDebugLogEntry(aborted.Tick, aborted.Text + "  (now " + title + ")", aborted.Kind);
                        abortEntryIndex = -1;
                        Version++;
                    }
                    return;
                case NodeStatus.Success:
                case NodeStatus.Failure:
                    if (!wasRunning) return;
                    Add(title + " " + AIGlyphs.Arrow + " " + status + ", OnEnd", status == NodeStatus.Success ? AIDebugLogKind.Success : AIDebugLogKind.Failure);
                    return;
                case NodeStatus.Aborted:
                    if (!wasRunning) return;
                    Add("ABORT " + (hasParent ? parentTitle + " " + AIGlyphs.Arrow + " " : string.Empty) + title + ".OnAbort, OnEnd", AIDebugLogKind.Abort);
                    abortEntryIndex = entries.Count - 1;
                    abortFrame = frame;
                    return;
            }
        }

        void Add(string text, AIDebugLogKind kind)
        {
            entries.Add(new AIDebugLogEntry(tick, text, kind));
            if (entries.Count > Capacity)
            {
                entries.RemoveAt(0);
                if (abortEntryIndex >= 0) abortEntryIndex--;
            }
            Version++;
        }
    }
}
