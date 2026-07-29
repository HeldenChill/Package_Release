using System;
using System.IO;
using UnityEngine;

namespace Hung.AutoTest
{
    public static class AutoTestFilePathUtility
    {
        public static string CreateTimestampRunId()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public static string GetReportFolder()
        {
#if UNITY_EDITOR
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, "AutoTestReports");
#else
            return Path.Combine(Application.persistentDataPath, "AutoTestReports");
#endif
        }
    }
}
