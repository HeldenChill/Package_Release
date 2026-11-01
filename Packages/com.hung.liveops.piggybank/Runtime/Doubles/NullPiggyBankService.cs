namespace Hung.LiveOps.PiggyBank
{
    using Hung.Base;

    // Test double (paper §17.7), same role as Ph6's NullAdsService/NullIapService: an
    // all-no-op IPiggyBankService for EditMode/PlayMode tests and for games that call the
    // piggy-bank contract without running the feature. Unlike PiggyBankManager it never
    // touches Database, so it needs no save round-trip, no prefab and no scene wiring.
    //
    // Progress stays 0 and IncreasePiggyProgress is dropped rather than counted in memory:
    // the real manager persists every increment through Database, so a double that counted
    // would report progress that silently vanishes on the next launch. Reporting "the
    // feature is off" is honest; reporting progress that no store backs is not.
    public class NullPiggyBankService : IPiggyBankService
    {
        public int PiggyGoldProgress => 0;
        public void IncreasePiggyProgress() { }
        public void ResetPiggyProgress() { }
    }
}
