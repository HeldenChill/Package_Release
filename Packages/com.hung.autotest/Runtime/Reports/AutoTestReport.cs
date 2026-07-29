using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.AutoTest
{
    [Serializable]
    public sealed class AutoTestReport
    {
        public string runId;
        public string startedAt;
        public string finishedAt;
        public string unityVersion;
        public string suiteId;
        public string suiteName;
        public AutoTestStatus status;
        public float durationSeconds;
        public AutoTestReportSummary summary = new AutoTestReportSummary();
        public List<AutoTestCaseReport> cases = new List<AutoTestCaseReport>();

        public static AutoTestReport Create(string runId, AutoTestSuiteData suite)
        {
            return new AutoTestReport
            {
                runId = runId,
                startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                unityVersion = Application.unityVersion,
                suiteId = suite != null ? suite.suiteId : string.Empty,
                suiteName = suite != null ? suite.displayName : string.Empty,
                status = AutoTestStatus.Running
            };
        }
    }

    [Serializable]
    public sealed class AutoTestCaseReport
    {
        public string testId;
        public string displayName;
        public string scenarioName;
        public AutoTestStatus status;
        public float durationSeconds;
        public int failureCount;
        public RuntimeSnapshot finalSnapshot;
        public List<AutoTestFailure> failures = new List<AutoTestFailure>();
        public List<AutoTestLogEntry> logs = new List<AutoTestLogEntry>();

        public static AutoTestCaseReport Create(AutoTestCaseData testCase)
        {
            return new AutoTestCaseReport
            {
                testId = testCase != null ? testCase.testId : string.Empty,
                displayName = testCase != null ? testCase.displayName : string.Empty,
                scenarioName = testCase != null && testCase.scenario != null ? testCase.scenario.name : string.Empty,
                status = AutoTestStatus.Running
            };
        }
    }

    [Serializable]
    public sealed class AutoTestReportSummary
    {
        public int totalCases;
        public int passedCases;
        public int failedCases;
        public int totalFailures;

        public static AutoTestReportSummary FromCases(List<AutoTestCaseReport> cases)
        {
            AutoTestReportSummary summary = new AutoTestReportSummary();

            if (cases == null)
                return summary;

            summary.totalCases = cases.Count;

            for (int i = 0; i < cases.Count; i++)
            {
                AutoTestCaseReport c = cases[i];
                if (c.status == AutoTestStatus.Passed)
                    summary.passedCases++;
                else
                    summary.failedCases++;

                summary.totalFailures += c.failureCount;
            }

            return summary;
        }
    }
}
