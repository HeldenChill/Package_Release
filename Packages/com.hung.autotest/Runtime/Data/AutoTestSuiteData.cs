using System.Collections.Generic;
using UnityEngine;

namespace Hung.AutoTest
{
    [CreateAssetMenu(fileName = "AutoTestSuite", menuName = "Tools/Auto Test/Auto Test Suite")]
    public sealed class AutoTestSuiteData : ScriptableObject
    {
        public string suiteId = "pvm_suite";
        public string displayName = "Auto Test Suite";
        public bool stopSuiteOnFirstFailure = true;
        public bool exportReportAfterRun = true;
        public string reportFileNamePrefix = "AutoTest_Report";
        public List<AutoTestCaseData> testCases = new List<AutoTestCaseData>();
    }
}
