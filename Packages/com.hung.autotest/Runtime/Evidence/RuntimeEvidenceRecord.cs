using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace Hung.AutoTest
{
    public enum RuntimeEvidenceResult { NotRun, Passed, Failed, Blocked }

    public enum RuntimeEvidenceAdapter { Fake, Unsupported, LevelPlay, Max, GooglePlay, Apple }

    [Serializable]
    public sealed class RuntimeEvidenceRecord
    {
        public string evidenceId;
        public string scenarioId;
        public string runId;
        public string sourceRevision;
        public string dirtyState;
        public string unityVersion;
        public string buildTarget;
        public string scriptingBackend;
        public string runtimeEnvironment;
        public RuntimeEvidenceAdapter adapter;
        public RuntimeEvidenceResult result;
        public string startedUtc;
        public string finishedUtc;
        public long elapsedMilliseconds;
        public string diagnosticCode;
        public List<RuntimeEvidenceAssertion> assertions = new List<RuntimeEvidenceAssertion>();
        public List<RuntimeEvidenceArtifact> artifacts = new List<RuntimeEvidenceArtifact>();

        [NonSerialized] private Stopwatch stopwatch;

        public static RuntimeEvidenceRecord Start(string scenarioId, string runId, RuntimeEvidenceAdapter adapter)
        {
            var now = DateTime.UtcNow;
            return new RuntimeEvidenceRecord
            {
                evidenceId = Guid.NewGuid().ToString("N"),
                scenarioId = scenarioId,
                runId = runId,
                sourceRevision = "unknown",
                dirtyState = "unknown",
                unityVersion = Application.unityVersion,
                buildTarget = Application.platform.ToString(),
                scriptingBackend = "unknown",
                runtimeEnvironment = Application.isEditor ? "Editor" : "Player",
                adapter = adapter,
                result = RuntimeEvidenceResult.NotRun,
                startedUtc = ToUtcString(now),
                stopwatch = Stopwatch.StartNew()
            };
        }

        public void AddAssertion(string name, bool passed, string observed)
        {
            assertions.Add(new RuntimeEvidenceAssertion
            {
                name = name,
                passed = passed,
                observed = RuntimeEvidenceRedactor.Redact(observed)
            });
        }

        public RuntimeEvidenceArtifact AddArtifact(string kind, string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var artifact = new RuntimeEvidenceArtifact
            {
                kind = kind,
                path = path,
                sha256 = RuntimeEvidenceWriter.ComputeSha256(bytes)
            };
            artifacts.Add(artifact);
            return artifact;
        }

        public void Complete(RuntimeEvidenceResult result, string diagnosticCode)
        {
            this.result = result;
            this.diagnosticCode = diagnosticCode;
            finishedUtc = ToUtcString(DateTime.UtcNow);
            elapsedMilliseconds = stopwatch != null ? stopwatch.ElapsedMilliseconds : 0;
        }

        private static string ToUtcString(DateTime utc)
        {
            return utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }

    [Serializable]
    public sealed class RuntimeEvidenceAssertion
    {
        public string name;
        public bool passed;
        public string observed;
    }

    [Serializable]
    public sealed class RuntimeEvidenceArtifact
    {
        public string kind;
        public string path;
        public string sha256;
    }
}
