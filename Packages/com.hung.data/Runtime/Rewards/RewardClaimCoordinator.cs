using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;

public sealed class RewardClaimCoordinator : IRewardClaimCoordinator
{
    private readonly IClock clock;
    private readonly RewardDayPolicy dayPolicy;
    private readonly string profileScope;
    private readonly IPersistenceService persistence;
    private readonly SaveDefinition<RewardIntegrityStateData> definition;
    private readonly IRewardGrantService grantService;

    public RewardClaimCoordinator(
        IClock clock,
        RewardDayPolicy dayPolicy,
        string profileScope,
        IPersistenceService persistence,
        SaveDefinition<RewardIntegrityStateData> definition,
        IRewardGrantService grantService)
    {
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.dayPolicy = dayPolicy;
        this.profileScope = string.IsNullOrWhiteSpace(profileScope) ? throw new ArgumentException("Profile scope cannot be empty.", nameof(profileScope)) : profileScope;
        this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.grantService = grantService ?? throw new ArgumentNullException(nameof(grantService));
    }

    public RewardClaimResult Claim(RewardClaimRequest request)
    {
        DateTime utcNow = ReadUtc();
        LoadResult<RewardIntegrityStateData> load = persistence.Load(definition);
        if (!load.Success)
            return new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, load.DiagnosticCode);

        RewardIntegrityStateData state = load.Value ?? RewardIntegrityStateData.CreateDefault();
        if (utcNow.Ticks < state.latestObservedUtcTicks)
            return new RewardClaimResult(RewardGrantOutcome.Unavailable, "CLOCK_ROLLBACK");

        RewardClaimRecordData existing = state.claims.FirstOrDefault(x => x.claimId == request.ClaimId.Value);
        if (existing != null)
        {
            if (!string.Equals(existing.payloadFingerprint, request.PayloadFingerprint, StringComparison.Ordinal))
                return new RewardClaimResult(RewardGrantOutcome.Conflict, "CLAIM_PAYLOAD_CONFLICT");
            if (existing.state == (int)RewardClaimStateData.Finalized || existing.state == (int)RewardClaimStateData.Granted)
                return new RewardClaimResult(RewardGrantOutcome.IdempotentReplay);
        }
        else
        {
            existing = CreateRecord(request, utcNow.Ticks, RewardClaimStateData.Prepared);
            state.claims.Add(existing);
            state.latestObservedUtcTicks = Math.Max(state.latestObservedUtcTicks, utcNow.Ticks);
            SaveResult prepared = persistence.Save(definition, Clone(state));
            if (!prepared.Success)
                return new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, prepared.DiagnosticCode);
        }

        existing.state = (int)RewardClaimStateData.Granting;
        existing.updatedUtcTicks = utcNow.Ticks;
        SaveResult granting = persistence.Save(definition, Clone(state));
        if (!granting.Success)
            return new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, granting.DiagnosticCode);

        RewardGrantResult grant = grantService.Grant(request.ClaimId, request.Items, request.PayloadFingerprint);
        if (grant.Outcome != RewardGrantOutcome.Success && grant.Outcome != RewardGrantOutcome.IdempotentReplay)
            return new RewardClaimResult(grant.Outcome, grant.DiagnosticCode);

        existing.state = (int)RewardClaimStateData.Granted;
        existing.recordedOutcome = (int)grant.Outcome;
        existing.updatedUtcTicks = utcNow.Ticks;
        SaveResult granted = persistence.Save(definition, Clone(state));
        return granted.Success
            ? new RewardClaimResult(grant.Outcome)
            : new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, granted.DiagnosticCode);
    }

    public RewardClaimResult Finalize(RewardClaimId id, Func<RewardFeatureCommitResult> persistFeatureState)
    {
        if (persistFeatureState == null)
            throw new ArgumentNullException(nameof(persistFeatureState));

        DateTime utcNow = ReadUtc();
        LoadResult<RewardIntegrityStateData> load = persistence.Load(definition);
        if (!load.Success)
            return new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, load.DiagnosticCode);

        RewardIntegrityStateData state = load.Value ?? RewardIntegrityStateData.CreateDefault();
        RewardClaimRecordData record = state.claims.FirstOrDefault(x => x.claimId == id.Value);
        if (record == null)
            return new RewardClaimResult(RewardGrantOutcome.InvalidReward, "CLAIM_NOT_FOUND");
        if (record.state == (int)RewardClaimStateData.Finalized)
            return new RewardClaimResult(RewardGrantOutcome.IdempotentReplay);
        if (record.state != (int)RewardClaimStateData.Granted)
            return new RewardClaimResult(RewardGrantOutcome.Unavailable, "CLAIM_NOT_GRANTED");

        RewardFeatureCommitResult feature = persistFeatureState();
        if (!feature.Success)
            return new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, feature.DiagnosticCode);

        record.state = (int)RewardClaimStateData.Finalized;
        record.finalizedUtcTicks = utcNow.Ticks;
        record.updatedUtcTicks = utcNow.Ticks;
        SaveResult save = persistence.Save(definition, Clone(state));
        return save.Success
            ? new RewardClaimResult(RewardGrantOutcome.Success)
            : new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, save.DiagnosticCode);
    }

    public RewardRecoveryReport RecoverPending()
    {
        LoadResult<RewardIntegrityStateData> load = persistence.Load(definition);
        if (!load.Success)
            return new RewardRecoveryReport(0, 1);

        RewardIntegrityStateData state = load.Value ?? RewardIntegrityStateData.CreateDefault();
        int recovered = 0;
        int failed = 0;
        foreach (RewardClaimRecordData record in state.claims.Where(x => x.state != (int)RewardClaimStateData.Finalized).ToList())
        {
            if (record.state == (int)RewardClaimStateData.Prepared)
            {
                recovered++;
                continue;
            }

            var items = record.items.Select(x => new RewardGrantItem(ItemId.Parse(x.itemId), x.quantity)).ToList();
            RewardGrantResult grant = grantService.Grant(new RewardClaimId(record.claimId), items, record.payloadFingerprint);
            if (grant.Outcome == RewardGrantOutcome.Success || grant.Outcome == RewardGrantOutcome.IdempotentReplay)
            {
                record.state = (int)RewardClaimStateData.Granted;
                record.recordedOutcome = (int)grant.Outcome;
                record.updatedUtcTicks = ReadUtc().Ticks;
                recovered++;
            }
            else
            {
                failed++;
            }
        }

        if (recovered > 0)
            persistence.Save(definition, Clone(state));
        return new RewardRecoveryReport(recovered, failed);
    }

    private DateTime ReadUtc()
    {
        DateTime utc = clock.UtcNow;
        if (utc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Reward coordinator clock must return UTC.");
        _ = dayPolicy.Resolve(utc);
        _ = profileScope;
        return utc;
    }

    private static RewardClaimRecordData CreateRecord(RewardClaimRequest request, long ticks, RewardClaimStateData state)
    {
        var record = RewardClaimRecordData.Prepared(request.ClaimId.Value, request.Feature, request.PayloadFingerprint, ticks);
        record.state = (int)state;
        foreach (RewardGrantItem item in request.Items.OrderBy(x => x.ItemId.Value, StringComparer.Ordinal))
            record.items.Add(new RewardGrantItemData(item.ItemId.Value, item.Quantity));
        return record;
    }

    private static RewardIntegrityStateData Clone(RewardIntegrityStateData source)
    {
        var clone = new RewardIntegrityStateData
        {
            latestObservedUtcTicks = source.latestObservedUtcTicks,
            resetOffsetMinutes = source.resetOffsetMinutes
        };
        foreach (RewardClaimRecordData claim in source.claims)
        {
            var copy = new RewardClaimRecordData
            {
                claimId = claim.claimId,
                feature = claim.feature,
                state = claim.state,
                payloadFingerprint = claim.payloadFingerprint,
                createdUtcTicks = claim.createdUtcTicks,
                updatedUtcTicks = claim.updatedUtcTicks,
                finalizedUtcTicks = claim.finalizedUtcTicks,
                recordedOutcome = claim.recordedOutcome
            };
            foreach (RewardGrantItemData item in claim.items)
                copy.items.Add(new RewardGrantItemData(item.itemId, item.quantity));
            clone.claims.Add(copy);
        }

        return clone;
    }
}
