using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Hung.Base.Tests
{
    /// <summary>Editor-only focused test runner that preserves raw XML across normal test-domain reloads.</summary>
    [InitializeOnLoad]
    public static class ServiceArchitectureTestRunner
    {
        private const string Pending="ServiceArchitecture.TestRunner.PendingXml";
        private static TestRunnerApi api;
        private static Callbacks callbacks;
        static ServiceArchitectureTestRunner()
        {
            if(!string.IsNullOrEmpty(SessionState.GetString(Pending,""))) Attach();
        }
        private static void Attach()
        {
            if(api!=null) return;
            api=ScriptableObject.CreateInstance<TestRunnerApi>(); callbacks=new Callbacks(); api.RegisterCallbacks(callbacks);
        }
        /// <summary>Runs only regex-matched fixtures in the current Editor, exporting actual NUnit XML.</summary>
        public static string Run(string fixtureRegex,string relativeXmlPath,bool playMode)
        {
            if(string.IsNullOrWhiteSpace(fixtureRegex)) throw new ArgumentException("Focused fixture regex required.",nameof(fixtureRegex));
            if(!string.IsNullOrEmpty(SessionState.GetString(Pending,""))) throw new InvalidOperationException("A service-architecture test run is pending.");
            string repo=Directory.GetParent(Application.dataPath).FullName;
            string boundary=Path.GetFullPath(Path.Combine(repo,"Docs/reports/horizon1-boundaries/service-architecture"))+Path.DirectorySeparatorChar;
            string xml=Path.GetFullPath(Path.Combine(repo,relativeXmlPath));
            if(!xml.StartsWith(boundary,StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("XML path outside evidence boundary.",nameof(relativeXmlPath));
            if(File.Exists(xml)) throw new IOException("Preserve existing XML evidence: "+xml);
            Directory.CreateDirectory(Path.GetDirectoryName(xml)); SessionState.SetString(Pending,xml); Attach();
            try { return api.Execute(new ExecutionSettings(new Filter
            { testMode=playMode?TestMode.PlayMode:TestMode.EditMode, groupNames=new[]{fixtureRegex} })); }
            catch { SessionState.EraseString(Pending); throw; }
        }
        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string path=SessionState.GetString(Pending,""); if(string.IsNullOrEmpty(path)) return;
                try { TestRunnerApi.SaveResultToFile(result,path); }
                finally
                {
                    SessionState.EraseString(Pending);
                    if(api!=null) { api.UnregisterCallbacks(this); UnityEngine.Object.DestroyImmediate(api); }
                    api=null; callbacks=null;
                }
            }
        }
    }
}
