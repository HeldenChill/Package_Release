using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Hung.Diagnostics.Editor
{
    public sealed class GameLogViewerWindow : EditorWindow
    {
        private sealed class LogEntry
        {
            public int sourceLine;

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

            public string scene;
            public string exception;
            public string stackTrace;

            public JObject json;
            public string rawJson;
            public string parseError;
        }

        private readonly string[] levelOptions =
        {
            "All",
            "Trace",
            "Debug",
            "Info",
            "Warning",
            "Error",
            "Fatal"
        };

        private List<string> logFiles = new();
        private List<LogEntry> entries = new();
        private List<LogEntry> filteredEntries = new();

        private int selectedFileIndex;
        private int selectedLevelIndex;

        private string selectedFilePath;
        private string areaFilter = string.Empty;
        private string eventFilter = string.Empty;
        private string flowIdFilter = string.Empty;
        private string messageFilter = string.Empty;

        private int maxLines = 3000;
        private bool autoReload;
        private double nextAutoReloadTime;

        private Vector2 listScroll;
        private Vector2 detailScroll;

        private int selectedEntryIndex = -1;

        private GUIStyle headerStyle;
        private GUIStyle smallMutedStyle;
        private GUIStyle rowButtonStyle;

        public static void Open()
        {
            var window = GetWindow<GameLogViewerWindow>();
            window.titleContent = new GUIContent("Game Log Viewer");
            window.minSize = new Vector2(980, 560);
            window.RefreshFileList(selectLatest: true);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshFileList(selectLatest: true);
        }

        private void OnGUI()
        {
            InitStyles();

            DrawToolbar();
            DrawFilters();
            DrawSummary();

            EditorGUILayout.Space(4);

            DrawLogTable();

            EditorGUILayout.Space(4);

            DrawSelectedDetail();

            HandleAutoReload();
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
            }

            if (smallMutedStyle == null)
            {
                smallMutedStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true
                };
            }

            if (rowButtonStyle == null)
            {
                rowButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh Files", GUILayout.Width(100)))
            {
                RefreshFileList(selectLatest: false);
            }

            if (GUILayout.Button("Load Latest", GUILayout.Width(90)))
            {
                RefreshFileList(selectLatest: true);
                LoadSelectedFile();
            }

            if (GUILayout.Button("Reload", GUILayout.Width(70)))
            {
                LoadSelectedFile();
            }

            if (GUILayout.Button("Open Folder", GUILayout.Width(95)))
            {
                GameLogEditorUtility.OpenLogFolder();
            }

            if (GUILayout.Button("Reveal File", GUILayout.Width(85)))
            {
                if (!string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath))
                {
                    EditorUtility.RevealInFinder(selectedFilePath);
                }
            }

            if (GUILayout.Button("Open File", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath))
                {
                    EditorUtility.OpenWithDefaultApp(selectedFilePath);
                }
            }

            if (GUILayout.Button("Copy Path", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(selectedFilePath))
                {
                    GUIUtility.systemCopyBuffer = selectedFilePath;
                    Debug.Log($"[GameDiagnostics] Copied log path: {selectedFilePath}");
                }
            }

            GUILayout.FlexibleSpace();

            autoReload = EditorGUILayout.ToggleLeft("Auto Reload", autoReload, GUILayout.Width(95));

            EditorGUILayout.LabelField("Max Lines", GUILayout.Width(60));
            maxLines = EditorGUILayout.IntField(maxLines, GUILayout.Width(70));
            maxLines = Mathf.Clamp(maxLines, 100, 100000);

            EditorGUILayout.EndHorizontal();

            DrawFilePopup();

            EditorGUILayout.EndVertical();
        }

        private void DrawFilePopup()
        {
            if (logFiles == null || logFiles.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No .jsonl log files found. Run the game first, or check Application.persistentDataPath/GameLogs.",
                    MessageType.Info
                );
                return;
            }

            string[] displayNames = logFiles
                .Select(GameLogEditorUtility.GetFriendlyFileName)
                .ToArray();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Log File", GUILayout.Width(60));

            int newIndex = EditorGUILayout.Popup(selectedFileIndex, displayNames);

            if (newIndex != selectedFileIndex)
            {
                selectedFileIndex = newIndex;
                selectedFilePath = logFiles[selectedFileIndex];
                LoadSelectedFile();
            }

            if (GUILayout.Button("Load", GUILayout.Width(60)))
            {
                selectedFilePath = logFiles[selectedFileIndex];
                LoadSelectedFile();
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                EditorGUILayout.SelectableLabel(
                    selectedFilePath,
                    smallMutedStyle,
                    GUILayout.Height(18)
                );
            }
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Filters", headerStyle);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Level", GUILayout.Width(40));
            selectedLevelIndex = EditorGUILayout.Popup(selectedLevelIndex, levelOptions, GUILayout.Width(90));

            EditorGUILayout.LabelField("Area", GUILayout.Width(35));
            areaFilter = EditorGUILayout.TextField(areaFilter, GUILayout.Width(130));

            EditorGUILayout.LabelField("Event", GUILayout.Width(40));
            eventFilter = EditorGUILayout.TextField(eventFilter, GUILayout.Width(180));

            EditorGUILayout.LabelField("Flow", GUILayout.Width(35));
            flowIdFilter = EditorGUILayout.TextField(flowIdFilter, GUILayout.Width(180));

            EditorGUILayout.LabelField("Message", GUILayout.Width(60));
            messageFilter = EditorGUILayout.TextField(messageFilter);

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                ClearFilters();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            ApplyFilters();
        }

        private void DrawSummary()
        {
            int warningCount = entries.Count(e => string.Equals(e.level, "Warning", StringComparison.OrdinalIgnoreCase));
            int errorCount = entries.Count(e => string.Equals(e.level, "Error", StringComparison.OrdinalIgnoreCase));
            int fatalCount = entries.Count(e => string.Equals(e.level, "Fatal", StringComparison.OrdinalIgnoreCase));
            int parseErrorCount = entries.Count(e => !string.IsNullOrEmpty(e.parseError));

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Loaded: {entries.Count}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Filtered: {filteredEntries.Count}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"Warnings: {warningCount}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"Errors: {errorCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"Fatal: {fatalCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Parse Errors: {parseErrorCount}", GUILayout.Width(120));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Copy Filtered Raw", GUILayout.Width(130)))
            {
                CopyFilteredRaw();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogTable()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawTableHeader();

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.MinHeight(220));

            for (int i = 0; i < filteredEntries.Count; i++)
            {
                DrawLogRow(i, filteredEntries[i]);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(45));
            GUILayout.Label("Frame", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Level", EditorStyles.boldLabel, GUILayout.Width(75));
            GUILayout.Label("Area", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("Event", EditorStyles.boldLabel, GUILayout.Width(190));
            GUILayout.Label("FlowId", EditorStyles.boldLabel, GUILayout.Width(160));
            GUILayout.Label("Message", EditorStyles.boldLabel);

            GUILayout.Label("", GUILayout.Width(45));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogRow(int filteredIndex, LogEntry entry)
        {
            bool isSelected = selectedEntryIndex == filteredIndex;

            Color oldColor = GUI.backgroundColor;

            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.55f, 0.75f, 1f, 0.65f);
            }
            else if (IsErrorLevel(entry.level))
            {
                GUI.backgroundColor = new Color(1f, 0.55f, 0.55f, 0.45f);
            }
            else if (IsWarningLevel(entry.level))
            {
                GUI.backgroundColor = new Color(1f, 0.9f, 0.45f, 0.35f);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUI.backgroundColor = oldColor;

            GUILayout.Label(entry.sequence.ToString(), GUILayout.Width(45));
            GUILayout.Label(entry.frame.ToString(), GUILayout.Width(70));
            GUILayout.Label(entry.level ?? "-", GUILayout.Width(75));
            GUILayout.Label(entry.area ?? "-", GUILayout.Width(120));
            GUILayout.Label(entry.eventName ?? "-", GUILayout.Width(190));
            GUILayout.Label(Shorten(entry.flowId, 22), GUILayout.Width(160));
            GUILayout.Label(entry.message ?? "-", GUILayout.MinWidth(160));

            if (GUILayout.Button("View", rowButtonStyle, GUILayout.Width(45)))
            {
                selectedEntryIndex = filteredIndex;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedDetail()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Selected Log Detail", headerStyle);

            if (selectedEntryIndex < 0 || selectedEntryIndex >= filteredEntries.Count)
            {
                EditorGUILayout.HelpBox("Select a log row to inspect its full JSON.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            LogEntry entry = filteredEntries[selectedEntryIndex];

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Copy Raw JSON", GUILayout.Width(110)))
            {
                GUIUtility.systemCopyBuffer = entry.rawJson ?? string.Empty;
            }

            if (GUILayout.Button("Copy Pretty JSON", GUILayout.Width(120)))
            {
                GUIUtility.systemCopyBuffer = GetPrettyJson(entry);
            }

            if (GUILayout.Button("Filter Same Flow", GUILayout.Width(120)))
            {
                flowIdFilter = entry.flowId ?? string.Empty;
                ApplyFilters();
            }

            if (GUILayout.Button("Filter Same Area", GUILayout.Width(120)))
            {
                areaFilter = entry.area ?? string.Empty;
                ApplyFilters();
            }

            if (GUILayout.Button("Filter Same Event", GUILayout.Width(125)))
            {
                eventFilter = entry.eventName ?? string.Empty;
                ApplyFilters();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            DrawDetailMeta(entry);

            EditorGUILayout.Space(4);

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll, GUILayout.Height(210));

            string detailText = GetPrettyJson(entry);

            EditorGUILayout.TextArea(detailText, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailMeta(LogEntry entry)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Time UTC", entry.timeUtc ?? "-");
            EditorGUILayout.LabelField("Session", entry.sessionId ?? "-");
            EditorGUILayout.LabelField("Scene", entry.scene ?? "-");
            EditorGUILayout.LabelField("Frame", entry.frame.ToString());
            EditorGUILayout.LabelField("Sequence", entry.sequence.ToString());
            EditorGUILayout.LabelField("Flow ID", entry.flowId ?? "-");
            EditorGUILayout.LabelField("Parent Flow ID", entry.parentFlowId ?? "-");

            if (!string.IsNullOrEmpty(entry.exception))
            {
                EditorGUILayout.LabelField("Exception", entry.exception);
            }

            if (!string.IsNullOrEmpty(entry.parseError))
            {
                EditorGUILayout.HelpBox(entry.parseError, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshFileList(bool selectLatest)
        {
            logFiles = GameLogEditorUtility.GetLogFiles();

            if (logFiles.Count == 0)
            {
                selectedFileIndex = 0;
                selectedFilePath = null;
                entries.Clear();
                filteredEntries.Clear();
                selectedEntryIndex = -1;
                return;
            }

            if (selectLatest)
            {
                selectedFileIndex = 0;
            }
            else
            {
                selectedFileIndex = Mathf.Clamp(selectedFileIndex, 0, logFiles.Count - 1);
            }

            selectedFilePath = logFiles[selectedFileIndex];

            LoadSelectedFile();
        }

        private void LoadSelectedFile()
        {
            entries.Clear();
            filteredEntries.Clear();
            selectedEntryIndex = -1;

            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
                return;

            List<string> lines = GameLogEditorUtility.ReadLastLines(selectedFilePath, maxLines);

            for (int i = 0; i < lines.Count; i++)
            {
                entries.Add(ParseLine(lines[i], i + 1));
            }

            ApplyFilters();

            Repaint();
        }

        private LogEntry ParseLine(string line, int sourceLine)
        {
            var entry = new LogEntry
            {
                sourceLine = sourceLine,
                rawJson = line
            };

            try
            {
                JObject json = JObject.Parse(line);

                entry.json = json;

                entry.timeUtc = json.Value<string>("timeUtc");
                entry.realtime = json.Value<float?>("realtime") ?? 0f;
                entry.frame = json.Value<int?>("frame") ?? 0;
                entry.sequence = json.Value<long?>("sequence") ?? 0;

                entry.sessionId = json.Value<string>("sessionId");
                entry.flowId = json.Value<string>("flowId");
                entry.parentFlowId = json.Value<string>("parentFlowId");

                entry.level = json.Value<string>("level");
                entry.area = json.Value<string>("area");
                entry.eventName = json.Value<string>("eventName");
                entry.message = json.Value<string>("message");

                entry.scene = json.Value<string>("scene");
                entry.exception = json.Value<string>("exception");
                entry.stackTrace = json.Value<string>("stackTrace");
            }
            catch (Exception e)
            {
                entry.level = "ParseError";
                entry.area = "Parser";
                entry.eventName = "Json.ParseFailed";
                entry.message = "Failed to parse JSONL line.";
                entry.parseError = e.Message;
            }

            return entry;
        }

        private void ApplyFilters()
        {
            IEnumerable<LogEntry> query = entries;

            string selectedLevel = levelOptions[Mathf.Clamp(selectedLevelIndex, 0, levelOptions.Length - 1)];

            if (!string.Equals(selectedLevel, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e =>
                    string.Equals(e.level, selectedLevel, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(areaFilter))
            {
                query = query.Where(e => ContainsIgnoreCase(e.area, areaFilter));
            }

            if (!string.IsNullOrWhiteSpace(eventFilter))
            {
                query = query.Where(e => ContainsIgnoreCase(e.eventName, eventFilter));
            }

            if (!string.IsNullOrWhiteSpace(flowIdFilter))
            {
                query = query.Where(e => ContainsIgnoreCase(e.flowId, flowIdFilter));
            }

            if (!string.IsNullOrWhiteSpace(messageFilter))
            {
                query = query.Where(e =>
                    ContainsIgnoreCase(e.message, messageFilter) ||
                    ContainsIgnoreCase(e.rawJson, messageFilter));
            }

            filteredEntries = query.ToList();

            if (selectedEntryIndex >= filteredEntries.Count)
            {
                selectedEntryIndex = -1;
            }
        }

        private void ClearFilters()
        {
            selectedLevelIndex = 0;
            areaFilter = string.Empty;
            eventFilter = string.Empty;
            flowIdFilter = string.Empty;
            messageFilter = string.Empty;

            ApplyFilters();
        }

        private void CopyFilteredRaw()
        {
            if (filteredEntries == null || filteredEntries.Count == 0)
            {
                GUIUtility.systemCopyBuffer = string.Empty;
                return;
            }

            string text = string.Join(
                Environment.NewLine,
                filteredEntries.Select(e => e.rawJson)
            );

            GUIUtility.systemCopyBuffer = text;

            Debug.Log($"[GameDiagnostics] Copied {filteredEntries.Count} filtered log line(s).");
        }

        private string GetPrettyJson(LogEntry entry)
        {
            if (entry == null)
                return string.Empty;

            if (entry.json == null)
                return entry.rawJson ?? string.Empty;

            try
            {
                return entry.json.ToString(Formatting.Indented);
            }
            catch
            {
                return entry.rawJson ?? string.Empty;
            }
        }

        private void HandleAutoReload()
        {
            if (!autoReload)
                return;

            if (EditorApplication.timeSinceStartup < nextAutoReloadTime)
                return;

            nextAutoReloadTime = EditorApplication.timeSinceStartup + 1.0d;

            LoadSelectedFile();
        }

        private static bool ContainsIgnoreCase(string source, string keyword)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(keyword))
                return false;

            return source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsErrorLevel(string level)
        {
            return string.Equals(level, "Error", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(level, "Fatal", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(level, "ParseError", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWarningLevel(string level)
        {
            return string.Equals(level, "Warning", StringComparison.OrdinalIgnoreCase);
        }

        private static string Shorten(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return "-";

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - 3) + "...";
        }
    }
}