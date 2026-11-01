using System.Text;
using UnityEditor;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Bottom strip listing the debugger's transition log, newest line kept in view.</summary>
    public sealed class AIConsolePanel : VisualElement
    {
        const float LineHeight = 18f;
        static readonly string[] KindClasses = { null, "ai-log-run", "ai-log-ok", "ai-log-fail", "ai-log-abort" };
        readonly ListView list;
        AIDebugLog log;
        int shownVersion = -1;

        public AIConsolePanel()
        {
            AddToClassList("ai-console");
            list = new ListView(null, LineHeight, MakeLine, BindLine) { selectionType = SelectionType.None };
            list.AddToClassList("ai-console-list");
            Add(list);
            this.AddManipulator(new ContextualMenuManipulator(BuildMenu));
        }

        public void SetLog(AIDebugLog value)
        {
            log = value;
            list.itemsSource = log?.Entries;
            shownVersion = -1;
            Refresh();
        }

        /// <summary>Cheap when nothing changed; the window calls it every poll.</summary>
        public void Refresh()
        {
            if (log == null || log.Version == shownVersion) return;
            shownVersion = log.Version;
            list.RefreshItems();
            if (log.Entries.Count > 0) list.ScrollToItem(log.Entries.Count - 1);
        }

        static VisualElement MakeLine()
        {
            var line = new VisualElement();
            line.AddToClassList("ai-console-line");
            var tick = new Label();
            tick.AddToClassList("ai-console-tick");
            tick.AddToClassList("ai-f-mono");
            var message = new Label();
            message.AddToClassList("ai-console-msg");
            message.AddToClassList("ai-f-mono");
            line.Add(tick);
            line.Add(message);
            return line;
        }

        void BindLine(VisualElement line, int index)
        {
            if (log == null || index < 0 || index >= log.Entries.Count) return;
            AIDebugLogEntry entry = log.Entries[index];
            ((Label)line[0]).text = "[tick " + entry.Tick.ToString("000") + "]";
            ((Label)line[1]).text = entry.Text;
            for (int i = 1; i < KindClasses.Length; i++) line.EnableInClassList(KindClasses[i], (int)entry.Kind == i);
        }

        void BuildMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Clear", _ =>
            {
                log?.Clear();
                Refresh();
            });
            evt.menu.AppendAction("Copy all", _ => EditorGUIUtility.systemCopyBuffer = AllText());
        }

        string AllText()
        {
            if (log == null) return string.Empty;
            var text = new StringBuilder();
            foreach (AIDebugLogEntry entry in log.Entries) text.Append("[tick ").Append(entry.Tick.ToString("000")).Append("] ").AppendLine(entry.Text);
            return text.ToString();
        }
    }
}
