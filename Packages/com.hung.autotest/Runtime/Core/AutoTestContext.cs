using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.AutoTest
{
    public sealed class AutoTestContext
    {
        public string RunId { get; private set; }
        public string SuiteId { get; private set; }
        public string TestId { get; private set; }
        public AutoTestCaseData CurrentCase { get; private set; }
        public AutoTestStatus Status { get; set; }
        public RuntimeSnapshot LatestSnapshot { get; set; }
        public AutoTestEventCollector Events { get; set; }
        public AutoTestFailure FatalFailure { get; private set; }
        public readonly List<AutoTestFailure> Failures = new List<AutoTestFailure>();
        public readonly List<AutoTestLogEntry> Logs = new List<AutoTestLogEntry>();

        public float StartedAtRealtime { get; private set; }
        public int FrameStarted { get; private set; }

        public float ElapsedSeconds
        {
            get { return Time.realtimeSinceStartup - StartedAtRealtime; }
        }

        public void Begin(string runId, string suiteId, AutoTestCaseData testCase)
        {
            RunId = runId;
            SuiteId = suiteId;
            CurrentCase = testCase;
            TestId = testCase != null ? testCase.testId : string.Empty;
            Status = AutoTestStatus.Running;
            LatestSnapshot = null;
            Events = null;
            FatalFailure = null;
            Failures.Clear();
            Logs.Clear();
            StartedAtRealtime = Time.realtimeSinceStartup;
            FrameStarted = Time.frameCount;
        }

        public void AddFailure(AutoTestFailure failure)
        {
            if (failure == null)
                return;

            if (string.IsNullOrEmpty(failure.contextSummary))
                failure.contextSummary = BuildContextSummary();

            Failures.Add(failure);

            if (failure.severity == AutoTestAssertionSeverity.Fatal && FatalFailure == null)
                FatalFailure = failure;
        }

        /// <summary>
        /// Game-state summary at failure time, for root-cause triage.
        /// Wrong phase → phase bug; no target + enemy in range → sensor bug;
        /// target + IDLE → FSM bug; detectionZoneCells 0 → init bug.
        /// </summary>
        private string BuildContextSummary()
        {
            RuntimeSnapshot s = LatestSnapshot;
            if (s == null)
                return "No snapshot captured at failure time.";

            var sb = new System.Text.StringBuilder(256);
            CombatSnapshot c = s.combat;
            sb.Append("Phase: ").Append(string.IsNullOrEmpty(c.gameplayPhase) ? "?" : c.gameplayPhase)
              .Append(" | Wave ").Append(c.waveIndex)
              .Append(" | Enemies ").Append(c.enemiesAlive).Append(" alive, ")
              .Append(c.enemiesSpawned).Append('/').Append(c.enemiesExpected).Append(" spawned, ")
              .Append(c.enemiesKilled).Append(" killed")
              .Append(" | Dmg ").Append(c.totalDamage.ToString("0"))
              .Append(" (DPS ").Append(c.dps.ToString("0.0")).Append(')');
            if (c.scenarioApplying)
                sb.Append(" | SCENARIO STILL APPLYING");

            for (int i = 0; i < s.pets.Count; i++)
            {
                PetSnapshot p = s.pets[i];
                sb.Append('\n').Append("Pet ").Append(p.petId)
                  .Append(": ").Append(string.IsNullOrEmpty(p.fsmState) ? "?" : p.fsmState)
                  .Append(", target=").Append(p.hasTarget ? p.targetName : "none");
                if (p.hasTarget)
                    sb.Append(" (dist ").Append(p.targetDistance.ToString("0.0"))
                      .Append(p.isOutOfRange ? ", OUT OF RANGE" : ", in range").Append(')');
                if (p.detectionZoneCells == 0)
                    sb.Append(", detectionZoneCells=0!");
            }

            return sb.ToString();
        }

        public bool HasFatalFailure
        {
            get { return FatalFailure != null; }
        }
    }
}
