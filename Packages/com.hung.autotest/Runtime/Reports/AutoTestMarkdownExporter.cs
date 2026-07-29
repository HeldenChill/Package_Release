using System.IO;
using System.Text;
using UnityEngine;

namespace Hung.AutoTest
{
    public static class AutoTestMarkdownExporter
    {
        public static string Export(AutoTestReport report, string folder, string fileNamePrefix)
        {
            if (report == null)
                return string.Empty;

            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, Sanitize(fileNamePrefix) + "_" + report.runId + ".md");
            File.WriteAllText(path, BuildMarkdown(report));
            Debug.Log("[AutoTestMarkdownExporter] Exported: " + path);
            return path;
        }

        private static string BuildMarkdown(AutoTestReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Auto Test Report");
            sb.AppendLine();
            sb.AppendLine("- Run: `" + report.runId + "`");
            sb.AppendLine("- Suite: `" + report.suiteName + "`");
            sb.AppendLine("- Status: **" + report.status + "**");
            sb.AppendLine("- Unity: `" + report.unityVersion + "`");
            sb.AppendLine("- Started: `" + report.startedAt + "`");
            sb.AppendLine("- Finished: `" + report.finishedAt + "`");
            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|---|---:|");
            sb.AppendLine("| Total Cases | " + report.summary.totalCases + " |");
            sb.AppendLine("| Passed | " + report.summary.passedCases + " |");
            sb.AppendLine("| Failed | " + report.summary.failedCases + " |");
            sb.AppendLine("| Failures | " + report.summary.totalFailures + " |");
            sb.AppendLine();

            for (int i = 0; i < report.cases.Count; i++)
            {
                AutoTestCaseReport c = report.cases[i];
                sb.AppendLine("## Case: " + c.displayName);
                sb.AppendLine();
                sb.AppendLine("- Test ID: `" + c.testId + "`");
                sb.AppendLine("- Scenario: `" + c.scenarioName + "`");
                sb.AppendLine("- Status: **" + c.status + "**");
                sb.AppendLine("- Duration: `" + c.durationSeconds.ToString("0.00") + "s`");
                sb.AppendLine();

                if (c.finalSnapshot != null)
                {
                    sb.AppendLine("### Combat Summary");
                    sb.AppendLine();
                    sb.AppendLine("| Metric | Value |");
                    sb.AppendLine("|---|---:|");
                    sb.AppendLine("| Wave Index | " + c.finalSnapshot.combat.waveIndex + " |");
                    sb.AppendLine("| Running | " + c.finalSnapshot.combat.isRunning + " |");
                    sb.AppendLine("| Enemies Expected | " + c.finalSnapshot.combat.enemiesExpected + " |");
                    sb.AppendLine("| Enemies Spawned | " + c.finalSnapshot.combat.enemiesSpawned + " |");
                    sb.AppendLine("| Enemies Killed | " + c.finalSnapshot.combat.enemiesKilled + " |");
                    sb.AppendLine("| Enemies Alive | " + c.finalSnapshot.combat.enemiesAlive + " |");
                    sb.AppendLine("| Waves Completed | " + c.finalSnapshot.combat.wavesCompleted + " |");
                    sb.AppendLine("| Total Damage | " + c.finalSnapshot.combat.totalDamage.ToString("0.###") + " |");
                    sb.AppendLine("| DPS | " + c.finalSnapshot.combat.dps.ToString("0.###") + " |");
                    sb.AppendLine("| DamageStat Record Calls | " + c.finalSnapshot.damageStatistics.totalRecordCalls + " |");
                    sb.AppendLine("| DamageStat Ignored No Manager | " + c.finalSnapshot.damageStatistics.totalIgnoredCallsNoManager + " |");
                    sb.AppendLine("| DamageStat Ignored No Session | " + c.finalSnapshot.damageStatistics.totalIgnoredCallsNoSession + " |");
                    sb.AppendLine();
                }

                if (c.failures.Count > 0)
                {
                    sb.AppendLine("### Failures");
                    sb.AppendLine();
                    for (int f = 0; f < c.failures.Count; f++)
                    {
                        AutoTestFailure failure = c.failures[f];
                        sb.AppendLine("- **" + failure.assertionId + "** [" + failure.severity + "]: " + failure.message);
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrEmpty(value) ? "AutoTestReport" : value.Replace(" ", "_");
        }
    }
}
