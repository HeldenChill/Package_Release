using System;
using Hung.Base;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Concrete <see cref="IEnergyService"/>. Constructed only by <see cref="EnergyServiceFactory"/>.
    /// Task 6 implements copy-on-write reconciliation/regeneration through the single
    /// <see cref="CommitCandidate"/> helper. Grant/run methods remain stubs pending Tasks 7-8.
    /// </summary>
    internal sealed class EnergyService : IEnergyService
    {
        private readonly IClock _clock;
        private readonly IEnergyStateStore _store;
        private readonly IEnergyConfigProvider _configProvider;

        private EnergyConfig _config;
        private string _configVersion;
        private EnergyState _state;
        private EnergySnapshot _current;

        public event Action<EnergySnapshot> Changed;

        /// <summary>
        /// Constructor now takes the live <see cref="IEnergyConfigProvider"/> (not a flat
        /// config/version snapshot) so <see cref="Reconcile"/> can re-check for config changes
        /// on every call, per the design doc's "re-check config each reconcile" requirement.
        /// Still performs its own initial <see cref="IEnergyConfigProvider.GetConfig"/> call
        /// synchronously to seed the first config/version — <see cref="EnergyServiceFactory"/>
        /// already validated this succeeds before constructing the service.
        /// </summary>
        public EnergyService(IClock clock, IEnergyStateStore store, IEnergyConfigProvider configProvider)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            EnergyConfigResult configResult = _configProvider.GetConfig();
            if (!configResult.Success)
            {
                throw new ArgumentException(
                    $"EnergyService: initial config read failed: {configResult.ErrorMessage}", nameof(configProvider));
            }
            _config = configResult.Config;
            _configVersion = configResult.Version;

            EnergyStateLoadResult loadResult = _store.Load();
            if (loadResult.Status == EnergyStateLoadStatus.Loaded)
            {
                _state = loadResult.State;
            }
            else
            {
                // ponytail: NotFound/Corrupt/UnsupportedVersion all fall back to a fresh state
                // seeded from config. Quarantine of corrupt payloads is the store's job (Task 4);
                // the service just needs a safe starting aggregate.
                _state = new EnergyState
                {
                    SchemaVersion = EnergyStateMapper.CurrentSchemaVersion,
                    RenewableAmount = _config.InitialRenewable,
                    BonusAmount = _config.InitialBonus,
                    RegenerationAnchorUtc = _clock.UtcNow,
                    UnlimitedUntilUtc = null,
                    LatestObservedUtc = _clock.UtcNow,
                    AppliedConfigSnapshot = _configVersion
                };
            }

            _current = BuildSnapshot(_state);
        }

        public EnergySnapshot Current => _current;

        public EnergyRunStartResult TryStartRun(string runId)
        {
            if (string.IsNullOrEmpty(runId))
            {
                return new EnergyRunStartResult(EnergyResultOutcome.InvalidInput, false, _current);
            }

            // Design §8.1 step 1: reconcile as its own separate commit before the run-start's
            // own commit (see report: two persists on the happy path, accepted tradeoff).
            Reconcile();

            string fingerprint = EnergyFingerprint.Compute($"try_start_run|{runId}");
            bool resultIsFree = false;

            EnergyCommandOutcome outcome = CommitCandidate(candidate =>
            {
                for (int i = 0; i < candidate.TransactionLedger.Count; i++)
                {
                    TransactionRecord existing = candidate.TransactionLedger[i];
                    if (existing.Id != runId)
                    {
                        continue;
                    }

                    if (existing.CommandKind == "TryStartRun" && existing.PayloadFingerprint == fingerprint)
                    {
                        resultIsFree = existing.IsFreeRun;
                        return EnergyCommandOutcome.NoChange(EnergyResultOutcome.IdempotentReplay);
                    }
                    return EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
                }

                if (candidate.ActiveRun != null && candidate.ActiveRun.RunId != runId)
                {
                    return EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
                }

                bool unlimitedActive = candidate.UnlimitedUntilUtc.HasValue && candidate.UnlimitedUntilUtc.Value > _clock.UtcNow;
                int reservedRenewable = 0;
                int reservedBonus = 0;

                if (unlimitedActive)
                {
                    candidate.ActiveRun = new ActiveRun
                    {
                        RunId = runId,
                        IsFree = true,
                        ReservedRenewable = 0,
                        ReservedBonus = 0,
                        EnteredGameplay = false
                    };
                }
                else
                {
                    int totalAvailable = candidate.RenewableAmount + candidate.BonusAmount;
                    if (totalAvailable < _config.RunCost)
                    {
                        // ponytail: insufficient attempts are not ledgered — nothing was
                        // reserved, so there's no state to make idempotent-safe.
                        return EnergyCommandOutcome.NoChange(EnergyResultOutcome.Insufficient);
                    }

                    reservedRenewable = Math.Min(candidate.RenewableAmount, _config.RunCost);
                    reservedBonus = _config.RunCost - reservedRenewable;
                    candidate.RenewableAmount -= reservedRenewable;
                    candidate.BonusAmount -= reservedBonus;
                    candidate.ActiveRun = new ActiveRun
                    {
                        RunId = runId,
                        IsFree = false,
                        ReservedRenewable = reservedRenewable,
                        ReservedBonus = reservedBonus,
                        EnteredGameplay = false
                    };
                }

                resultIsFree = candidate.ActiveRun.IsFree;
                AppendLedgerRecord(candidate, runId, "TryStartRun", fingerprint, "Success",
                    string.Empty, reservedRenewable, reservedBonus, resultIsFree);

                return EnergyCommandOutcome.Persist(EnergyResultOutcome.Success);
            }, out EnergySnapshot snapshot);

            return new EnergyRunStartResult(outcome.Outcome, resultIsFree, snapshot);
        }

        public EnergyRunEntryResult MarkRunEntered(string runId)
        {
            string fingerprint = EnergyFingerprint.Compute($"mark_run_entered|{runId}");

            EnergyCommandOutcome outcome = CommitCandidate(candidate =>
            {
                if (candidate.ActiveRun == null || candidate.ActiveRun.RunId != runId)
                {
                    return EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
                }

                if (candidate.ActiveRun.EnteredGameplay)
                {
                    return EnergyCommandOutcome.NoChange(EnergyResultOutcome.IdempotentReplay);
                }

                candidate.ActiveRun.EnteredGameplay = true;
                AppendLedgerRecord(candidate, runId, "MarkRunEntered", fingerprint, "Success",
                    string.Empty, 0, 0, candidate.ActiveRun.IsFree);

                return EnergyCommandOutcome.Persist(EnergyResultOutcome.Success);
            }, out EnergySnapshot snapshot);

            return new EnergyRunEntryResult(outcome.Outcome);
        }

        public EnergyRunCompletionResult CompleteRun(string runId, RunOutcome outcome)
        {
            string fingerprint = EnergyFingerprint.Compute($"complete_run|{runId}|{outcome}");
            RunOutcome? recordedOutcome = null;

            EnergyCommandOutcome commandOutcome = CommitCandidate(candidate =>
            {
                if (candidate.ActiveRun == null || candidate.ActiveRun.RunId != runId)
                {
                    TransactionRecord finalized = FindLatestCompleteRunRecord(candidate, runId);
                    if (finalized == null)
                    {
                        return EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
                    }

                    RunOutcome originalOutcome = (RunOutcome)Enum.Parse(typeof(RunOutcome), finalized.RunOutcome);
                    recordedOutcome = originalOutcome;
                    return originalOutcome == outcome
                        ? EnergyCommandOutcome.NoChange(EnergyResultOutcome.IdempotentReplay)
                        : EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
                }

                ActiveRun activeRun = candidate.ActiveRun;
                candidate.ActiveRun = null;
                recordedOutcome = outcome;
                AppendLedgerRecord(candidate, runId, "CompleteRun", fingerprint, "Success",
                    outcome.ToString(), activeRun.ReservedRenewable, activeRun.ReservedBonus, activeRun.IsFree);

                return EnergyCommandOutcome.Persist(EnergyResultOutcome.Success);
            }, out EnergySnapshot snapshot);

            return new EnergyRunCompletionResult(commandOutcome.Outcome, recordedOutcome, snapshot);
        }

        public EnergyRunCancellationResult CancelFailedStart(string runId)
        {
            string fingerprint = EnergyFingerprint.Compute($"cancel_failed_start|{runId}");

            EnergyCommandOutcome outcome = CommitCandidate(candidate =>
            {
                if (candidate.ActiveRun == null || candidate.ActiveRun.RunId != runId)
                {
                    for (int i = 0; i < candidate.TransactionLedger.Count; i++)
                    {
                        TransactionRecord existing = candidate.TransactionLedger[i];
                        if (existing.Id == runId && existing.CommandKind == "CancelFailedStart")
                        {
                            return EnergyCommandOutcome.NoChange(EnergyResultOutcome.IdempotentReplay);
                        }
                    }
                    return EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
                }

                if (candidate.ActiveRun.EnteredGameplay)
                {
                    return EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
                }

                ActiveRun activeRun = candidate.ActiveRun;
                candidate.RenewableAmount += activeRun.ReservedRenewable;
                candidate.BonusAmount += activeRun.ReservedBonus;
                candidate.ActiveRun = null;
                AppendLedgerRecord(candidate, runId, "CancelFailedStart", fingerprint, "Success",
                    string.Empty, activeRun.ReservedRenewable, activeRun.ReservedBonus, activeRun.IsFree);

                return EnergyCommandOutcome.Persist(EnergyResultOutcome.Success);
            }, out EnergySnapshot snapshot);

            return new EnergyRunCancellationResult(outcome.Outcome, snapshot);
        }

        /// <summary>Shared ledger-append + eviction, mirroring <see cref="MutateGrant"/>'s pattern for run commands.</summary>
        private void AppendLedgerRecord(
            EnergyState candidate, string id, string commandKind, string fingerprint, string outcome,
            string runOutcome, int reservedRenewable, int reservedBonus, bool isFreeRun)
        {
            candidate.TransactionLedger.Add(new TransactionRecord
            {
                Id = id,
                CommandKind = commandKind,
                PayloadFingerprint = fingerprint,
                Outcome = outcome,
                RunOutcome = runOutcome,
                ReservedRenewable = reservedRenewable,
                ReservedBonus = reservedBonus,
                IsFreeRun = isFreeRun
            });

            while (candidate.TransactionLedger.Count > _config.TransactionRetentionCapacity)
            {
                candidate.TransactionLedger.RemoveAt(0);
            }
        }

        /// <summary>Finds the most recent finalized <c>CompleteRun</c> ledger record for a runId, if any.</summary>
        private static TransactionRecord FindLatestCompleteRunRecord(EnergyState candidate, string runId)
        {
            for (int i = candidate.TransactionLedger.Count - 1; i >= 0; i--)
            {
                TransactionRecord existing = candidate.TransactionLedger[i];
                if (existing.Id == runId && existing.CommandKind == "CompleteRun")
                {
                    return existing;
                }
            }
            return null;
        }

        public EnergyGrantResult AddRenewable(int amount, string transactionId)
        {
            if (amount <= 0)
            {
                return new EnergyGrantResult(EnergyResultOutcome.InvalidInput, _current);
            }

            string canonical = $"AddRenewable|{amount}";
            string fingerprint = EnergyFingerprint.Compute(canonical);

            EnergyCommandOutcome outcome = CommitCandidate(
                candidate => MutateGrant(candidate, transactionId, "AddRenewable", fingerprint,
                    c => c.RenewableAmount = Math.Min(_config.RenewableMax, c.RenewableAmount + amount)),
                out EnergySnapshot snapshot);
            return new EnergyGrantResult(outcome.Outcome, snapshot);
        }

        public EnergyGrantResult AddBonus(int amount, string transactionId)
        {
            if (amount <= 0)
            {
                return new EnergyGrantResult(EnergyResultOutcome.InvalidInput, _current);
            }

            string canonical = $"AddBonus|{amount}";
            string fingerprint = EnergyFingerprint.Compute(canonical);

            EnergyCommandOutcome outcome = CommitCandidate(
                candidate => MutateGrant(candidate, transactionId, "AddBonus", fingerprint,
                    c => c.BonusAmount += amount),
                out EnergySnapshot snapshot);
            return new EnergyGrantResult(outcome.Outcome, snapshot);
        }

        public EnergyGrantResult GrantUnlimited(TimeSpan duration, string transactionId)
        {
            if (duration <= TimeSpan.Zero)
            {
                return new EnergyGrantResult(EnergyResultOutcome.InvalidInput, _current);
            }

            string canonical = $"grant_unlimited|{duration.Ticks}";
            string fingerprint = EnergyFingerprint.Compute(canonical);

            EnergyCommandOutcome outcome = CommitCandidate(
                candidate => MutateGrant(candidate, transactionId, "GrantUnlimited", fingerprint,
                    c =>
                    {
                        DateTime effectiveNow = _clock.UtcNow > c.LatestObservedUtc
                            ? _clock.UtcNow
                            : (c.LatestObservedUtc ?? _clock.UtcNow);
                        DateTime baseTime = c.UnlimitedUntilUtc.HasValue && c.UnlimitedUntilUtc.Value > effectiveNow
                            ? c.UnlimitedUntilUtc.Value
                            : effectiveNow;
                        c.UnlimitedUntilUtc = baseTime + duration;
                    }),
                out EnergySnapshot snapshot);
            return new EnergyGrantResult(outcome.Outcome, snapshot);
        }

        /// <summary>
        /// Shared idempotent-grant path: check the ledger for a matching/conflicting prior
        /// record by transaction id, otherwise apply <paramref name="applyMutation"/>, append
        /// a ledger record, and evict oldest entries beyond retention capacity.
        /// </summary>
        private EnergyCommandOutcome MutateGrant(
            EnergyState candidate,
            string transactionId,
            string commandKind,
            string fingerprint,
            Action<EnergyState> applyMutation)
        {
            for (int i = 0; i < candidate.TransactionLedger.Count; i++)
            {
                TransactionRecord existing = candidate.TransactionLedger[i];
                if (existing.Id != transactionId)
                {
                    continue;
                }

                bool matches = existing.CommandKind == commandKind && existing.PayloadFingerprint == fingerprint;
                return matches
                    ? EnergyCommandOutcome.NoChange(EnergyResultOutcome.IdempotentReplay)
                    : EnergyCommandOutcome.NoChange(EnergyResultOutcome.Conflict);
            }

            applyMutation(candidate);

            candidate.TransactionLedger.Add(new TransactionRecord
            {
                Id = transactionId,
                CommandKind = commandKind,
                PayloadFingerprint = fingerprint,
                Outcome = "Success",
                RunOutcome = string.Empty,
                ReservedRenewable = 0,
                ReservedBonus = 0,
                IsFreeRun = false
            });

            while (candidate.TransactionLedger.Count > _config.TransactionRetentionCapacity)
            {
                candidate.TransactionLedger.RemoveAt(0);
            }

            return EnergyCommandOutcome.Persist(EnergyResultOutcome.Success);
        }

        public EnergySnapshot Reconcile()
        {
            CommitCandidate(MutateReconcile, out EnergySnapshot snapshot);
            return snapshot;
        }

        /// <summary>
        /// The single copy-on-write mutation path: clone current state, run <paramref name="mutate"/>
        /// against the candidate, persist only if the mutation says to, and publish only after a
        /// successful save. Every command method (this task's Reconcile, and Tasks 7-8's grant/run
        /// methods) must route through this helper — it is the only place <see cref="_state"/> is
        /// reassigned or <see cref="Changed"/> is raised.
        /// </summary>
        private EnergyCommandOutcome CommitCandidate(
            Func<EnergyState, EnergyCommandOutcome> mutate,
            out EnergySnapshot snapshot)
        {
            EnergyState candidate = _state.DeepClone();
            EnergyCommandOutcome outcome = mutate(candidate);
            if (!outcome.ShouldPersist)
            {
                snapshot = _current = BuildSnapshot(_state);
                return outcome;
            }
            if (!_store.Save(candidate))
            {
                snapshot = _current = BuildSnapshot(_state);
                return EnergyCommandOutcome.PersistenceFailure;
            }
            _state = candidate;
            snapshot = _current = BuildSnapshot(_state);
            Changed?.Invoke(snapshot);
            return outcome;
        }

        /// <summary>
        /// Design doc section 6's 8-step reconciliation algorithm, plus the config-change
        /// bullet from Task 6: reconcile with the previously applied config, persist, then
        /// adopt the new config. Returns ShouldPersist=false when nothing observable changed
        /// (idle reconcile is a no-op — does not hit the store or fire Changed).
        /// </summary>
        private EnergyCommandOutcome MutateReconcile(EnergyState candidate)
        {
            bool changed = false;

            // Step 1: read current UTC.
            DateTime now = _clock.UtcNow;

            // Step 2: rollback detection. Grant no time-derived progress; do not move
            // LatestObservedUtc backward. RollbackDetected is derived at snapshot time by
            // comparing `now` against candidate.LatestObservedUtc, so no separate flag field
            // is needed on the state itself.
            bool rollback = candidate.LatestObservedUtc.HasValue && now < candidate.LatestObservedUtc.Value;

            if (!rollback)
            {
                // Config-change check: re-read the provider and compare version strings.
                // Regeneration math below always uses `_config` (the previously-applied,
                // still-active config) for this call; only after this mutation commits do we
                // swap `_config`/`_configVersion` to the new values (see below, outside this
                // per-candidate mutation, since config is a service-level field, not aggregate
                // state — see report for reasoning on this split).
                changed |= ApplyRegeneration(candidate, now);
                changed |= ApplyUnlimitedExpiry(candidate, now);

                if (candidate.LatestObservedUtc != now)
                {
                    candidate.LatestObservedUtc = now;
                    changed = true;
                }
            }
            else
            {
                // Rollback: LatestObservedUtc intentionally left untouched (not advanced
                // backward). Still update the anchor is NOT allowed either. RollbackDetected
                // becomes visible on the snapshot via BuildSnapshot's own now-vs-LatestObservedUtc
                // comparison — no mutation needed here beyond leaving state as-is.
            }

            // Config adoption: detect after the time-based math above so the elapsed time
            // between the last reconcile and now is always settled under the config that was
            // in effect for that whole interval, per the plan's Task 6 bullet.
            EnergyConfigResult configResult = _configProvider.GetConfig();
            if (configResult.Success && configResult.Version != candidate.AppliedConfigSnapshot)
            {
                candidate.AppliedConfigSnapshot = configResult.Version;
                changed = true;
                // Adopt the new config for subsequent calls only after this candidate commits.
                // Since CommitCandidate persists candidate unconditionally when ShouldPersist,
                // and _config/_configVersion are service fields (not part of EnergyState), we
                // swap them right here — by the time this mutation returns, `candidate` (which
                // becomes the new `_state` on success) already reflects the version used for
                // config recognition, and _config below matches it for all future calls.
                _config = configResult.Config;
                _configVersion = configResult.Version;
            }

            return changed
                ? EnergyCommandOutcome.Persist(EnergyResultOutcome.Success)
                : EnergyCommandOutcome.NoChange(EnergyResultOutcome.Success);
        }

        /// <summary>
        /// Steps 3-5: complete-interval regeneration with remainder preservation, capped at
        /// RenewableMax, and the full-balance anchor-snap exploit guard.
        /// </summary>
        private bool ApplyRegeneration(EnergyState candidate, DateTime now)
        {
            if (candidate.RenewableAmount >= _config.RenewableMax)
            {
                // Anti-exploit: snap the anchor to now whenever renewable is already at cap so
                // stockpiled elapsed time cannot fund an immediate refill after spending later.
                if (candidate.RegenerationAnchorUtc != now)
                {
                    candidate.RegenerationAnchorUtc = now;
                    return true;
                }
                return false;
            }

            DateTime anchor = candidate.RegenerationAnchorUtc ?? now;
            if (now <= anchor)
            {
                return false;
            }

            TimeSpan elapsed = now - anchor;
            TimeSpan interval = _config.RegenerationInterval;
            long completeIntervals = elapsed.Ticks / interval.Ticks;
            if (completeIntervals <= 0)
            {
                return false;
            }

            int room = _config.RenewableMax - candidate.RenewableAmount;
            long intervalsToApply = Math.Min(completeIntervals, room);
            if (intervalsToApply < 0)
            {
                intervalsToApply = 0;
            }

            bool changed = false;
            if (intervalsToApply > 0)
            {
                candidate.RenewableAmount += (int)intervalsToApply;
                changed = true;
            }

            // Preserve remainder: advance anchor only by the complete intervals actually
            // consumed by the elapsed time (not all the way to `now`), UNLESS applying them
            // just filled renewable to cap — in that case snap to now (same anti-exploit as
            // above) so the discarded room-limited intervals cannot be re-spent later.
            DateTime advancedAnchor = anchor + TimeSpan.FromTicks(interval.Ticks * completeIntervals);
            if (candidate.RenewableAmount >= _config.RenewableMax)
            {
                if (candidate.RegenerationAnchorUtc != now)
                {
                    candidate.RegenerationAnchorUtc = now;
                    changed = true;
                }
            }
            else if (candidate.RegenerationAnchorUtc != advancedAnchor)
            {
                candidate.RegenerationAnchorUtc = advancedAnchor;
                changed = true;
            }

            return changed;
        }

        /// <summary>Step 6: clear Unlimited Energy once its absolute expiry has passed.</summary>
        private static bool ApplyUnlimitedExpiry(EnergyState candidate, DateTime now)
        {
            if (candidate.UnlimitedUntilUtc.HasValue && now >= candidate.UnlimitedUntilUtc.Value)
            {
                candidate.UnlimitedUntilUtc = null;
                return true;
            }
            return false;
        }

        private EnergySnapshot BuildSnapshot(EnergyState state)
        {
            bool isUnlimitedActive = state.UnlimitedUntilUtc.HasValue && state.UnlimitedUntilUtc.Value > _clock.UtcNow;

            DateTime? nextRegeneration = null;
            if (state.RenewableAmount < _config.RenewableMax && state.RegenerationAnchorUtc.HasValue)
            {
                nextRegeneration = state.RegenerationAnchorUtc.Value + _config.RegenerationInterval;
            }

            bool rollbackDetected = state.LatestObservedUtc.HasValue && _clock.UtcNow < state.LatestObservedUtc.Value;

            EnergyActiveRunSnapshot activeRun = state.ActiveRun == null
                ? EnergyActiveRunSnapshot.None
                : new EnergyActiveRunSnapshot(true, state.ActiveRun.RunId, state.ActiveRun.IsFree, state.ActiveRun.EnteredGameplay);

            return new EnergySnapshot(
                renewableAmount: state.RenewableAmount,
                bonusAmount: state.BonusAmount,
                renewableMax: _config.RenewableMax,
                isUnlimitedActive: isUnlimitedActive,
                unlimitedExpiryUtc: state.UnlimitedUntilUtc,
                nextRegenerationUtc: nextRegeneration,
                rollbackDetected: rollbackDetected,
                activeRun: activeRun);
        }
    }
}
