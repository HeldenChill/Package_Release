using UnityEditor;

namespace Hung.Diagnostics.Editor
{
    public static class GameDiagnosticsEditorMenu
    {
        private const string RootMenu = "Tools/Game Diagnostics/";

        [MenuItem(RootMenu + "Open Log Viewer")]
        public static void OpenLogViewer()
        {
            GameLogViewerWindow.Open();
        }

        [MenuItem(RootMenu + "Open Log Folder")]
        public static void OpenLogFolder()
        {
            GameLogEditorUtility.OpenLogFolder();
        }

        [MenuItem(RootMenu + "Open Latest Log File")]
        public static void OpenLatestLogFile()
        {
            GameLogEditorUtility.OpenLatestLogFile();
        }

        [MenuItem(RootMenu + "Reveal Latest Log File")]
        public static void RevealLatestLogFile()
        {
            GameLogEditorUtility.RevealLatestLogFile();
        }

        [MenuItem(RootMenu + "Copy Latest Log Path")]
        public static void CopyLatestLogPath()
        {
            GameLogEditorUtility.CopyLatestLogPath();
        }

        [MenuItem(RootMenu + "Clear All Logs")]
        public static void ClearAllLogs()
        {
            GameLogEditorUtility.ClearAllLogs();
        }
    }
}