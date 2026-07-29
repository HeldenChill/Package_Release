using System.IO;
using UnityEngine;

namespace Hung.AutoTest
{
    public static class AutoTestJsonExporter
    {
        public static string Export(AutoTestReport report, string folder, string fileNamePrefix)
        {
            if (report == null)
                return string.Empty;

            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, Sanitize(fileNamePrefix) + "_" + report.runId + ".json");
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(path, json);
            Debug.Log("[AutoTestJsonExporter] Exported: " + path);
            return path;
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrEmpty(value) ? "AutoTestReport" : value.Replace(" ", "_");
        }
    }
}
