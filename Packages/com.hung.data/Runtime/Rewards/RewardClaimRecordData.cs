using System;
using System.Collections.Generic;

public enum RewardClaimStateData
{
    Prepared = 1,
    Granting = 2,
    Granted = 3,
    Finalized = 4
}

[Serializable]
public sealed class RewardClaimRecordData
{
    public string claimId;
    public string feature;
    public int state;
    public string payloadFingerprint;
    public List<RewardGrantItemData> items = new();
    public long createdUtcTicks;
    public long updatedUtcTicks;
    public long finalizedUtcTicks;
    public int recordedOutcome;

    public static RewardClaimRecordData Prepared(string claimId, string feature, string payloadFingerprint, long utcTicks)
    {
        return new RewardClaimRecordData
        {
            claimId = claimId,
            feature = feature,
            state = (int)RewardClaimStateData.Prepared,
            payloadFingerprint = payloadFingerprint,
            createdUtcTicks = utcTicks,
            updatedUtcTicks = utcTicks
        };
    }
}
