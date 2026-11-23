using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hung.Diagnostics.Editor
{
    public static class GameLogEditorUtility
    {
        public static string LogDirectoryPath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "GameLogs");
            }
        }

        public static void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(LogDirectoryPath))
            {
                Directory.CreateDirectory(LogDirectoryPath);
            }
        }

        public static List<string> GetLogFiles()
        {
            EnsureLogDirectoryExists();

            return Directory
                .GetFiles(LogDirectoryPath, "*.jsonl", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();
        }

        public static string GetLatestLogFile()
        {
            List<string> files = GetLogFiles();

            if (files == null || files.Count == 0)
                return null;

            return files[0];
        }

        public static void OpenLogFolder()
        {
            EnsureLogDirectoryExists();
            EditorUtility.RevealInFinder(LogDirectoryPath);
        }

        public static void OpenLatestLogFile()
        {
            string latest = GetLatestLogFile();

            if (string.IsNullOrEmpty(latest) || !File.Exists(latest))
            {
                EditorUtility.DisplayDialog(
                    "Game Diagnostics",
                    "No log file found.",
                    "OK"
                );
                return;
            }

            EditorUtility.OpenWithDefaultApp(latest);
        }

        public static void RevealLatestLogFile()
        {
            string latest = GetLatestLogFile();

            if (string.IsNullOrEmpty(latest) || !File.Exists(latest))
            {
                EditorUtility.DisplayDialog(
                    "Game Diagnostics",
                    "No log file found.",
                    "OK"
                );
                return;
            }

            EditorUtility.RevealInFinder(latest);
        }

        public static void CopyLatestLogPath()
        {
            string latest = GetLatestLogFile();

            if (string.IsNullOrEmpty(latest) || !File.Exists(latest))
            {
                EditorUtility.DisplayDialog(
                    "Game Diagnostics",
                    "No log file found.",
                    "OK"
                );
                return;
            }

            GUIUtility.systemCopyBuffer = latest;

            Debug.Log($"[GameDiagnostics] Copied latest log path: {latest}");
        }

        public static void ClearAllLogs()
        {
            EnsureLogDirectoryExists();

            bool confirm = EditorUtility.DisplayDialog(
                "Clear Game Logs",
                "Delete all .jsonl log files in GameLogs folder?",
                "Delete",
                "Cancel"
            );

            if (!confirm)
                return;

            string[] files = Directory.GetFiles(
                LogDirectoryPath,
                "*.jsonl",
                SearchOption.TopDirectoryOnly
            );

            int deletedCount = 0;

            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GameDiagnostics] Failed to delete log file: {file}\n{e}");
                }
            }

            Debug.Log($"[GameDiagnostics] Deleted {deletedCount} log file(s).");
        }

        public static string GetFriendlyFileName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "None";

            return Path.GetFileName(filePath);
        }

        public static List<string> ReadLastLines(string filePath, int maxLines)
        {
            var result = new Queue<string>();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return result.ToList();

            maxLines = Mathf.Max(1, maxLines);

            foreach (string line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                result.Enqueue(line);

                while (result.Count > maxLines)
                {
                    result.Dequeue();
                }
            }

            return result.ToList();
        }
    }
}