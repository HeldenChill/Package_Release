namespace Hung.Base
{
    public enum ItemSpendOutcome
    {
        Unavailable = 0,
        Success,
        InvalidRequest,
        UnknownItem,
        InsufficientBalance,
        PersistenceFailure
    }

    public readonly struct ItemSpendResult
    {
        public ItemSpendResult(ItemSpendOutcome outcome, string diagnosticCode = null)
        {
            Outcome = outcome;
            DiagnosticCode = diagnosticCode;
        }

        public ItemSpendOutcome Outcome { get; }
        public string DiagnosticCode { get; }
    }

    public interface IItemSpendService
    {
        // Positive quantity only. Success means durable commit, then publication.
        // Failed operations must leave authoritative in-memory balances unchanged.
        ItemSpendResult Spend(ItemId itemId, int quantity);
    }
}
