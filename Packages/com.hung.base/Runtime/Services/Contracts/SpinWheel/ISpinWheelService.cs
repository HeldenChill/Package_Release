using System;

namespace Hung.Base
{
    public readonly struct SpinAvailability
    {
        public SpinAvailability(bool freeAvailable, int adsSpinToday, int adsSpinLimit, DateTime nextResetUtc)
        {
            FreeAvailable = freeAvailable;
            AdsSpinToday = adsSpinToday;
            AdsSpinLimit = adsSpinLimit;
            NextResetUtc = nextResetUtc;
        }

        public bool FreeAvailable { get; }
        public int AdsSpinToday { get; }
        public int AdsSpinLimit { get; }
        public DateTime NextResetUtc { get; }
        public bool AdsAvailable => AdsSpinToday < AdsSpinLimit;
    }

    public readonly struct SpinStartResult
    {
        public SpinStartResult(RewardGrantOutcome outcome, string spinId = null, int selectedIndex = -1, string diagnosticCode = null)
        {
            Outcome = outcome;
            SpinId = spinId;
            SelectedIndex = selectedIndex;
            DiagnosticCode = diagnosticCode;
        }

        public RewardGrantOutcome Outcome { get; }
        public string SpinId { get; }
        public int SelectedIndex { get; }
        public string DiagnosticCode { get; }
        public bool Success => Outcome == RewardGrantOutcome.Success || Outcome == RewardGrantOutcome.IdempotentReplay;
    }

    public interface ISpinWheelService 
    {
        SpinAvailability Current { get; }
        SpinStartResult PrepareFreeSpin(int selectedIndex);
        SpinStartResult PrepareAuthorizedSpin(int selectedIndex, RewardAuthorization authorization);
        RewardClaimResult CompleteSpin(string spinId);
        DateTime NextResetUtc { get; }

        bool IsDoneSpinFreeToday { get; }
        int AdsSpinToday { get; }
        int DayOfYear { get; }
    }
}
