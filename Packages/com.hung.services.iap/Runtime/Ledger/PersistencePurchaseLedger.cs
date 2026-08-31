using System;
using Hung.Base.Persistence;

namespace Hung.IAP
{
    public sealed class PersistencePurchaseLedger : IPurchaseLedger
    {
        private readonly IPersistenceService persistence;
        private readonly SaveDefinition<PurchaseLedgerState> definition;
        private PurchaseLedgerState state;

        private PersistencePurchaseLedger(
            IPersistenceService persistence,
            SaveDefinition<PurchaseLedgerState> definition,
            PurchaseLedgerState state,
            bool isAvailable,
            SaveRecoveryState loadRecovery)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.state = Clone(state ?? new PurchaseLedgerState());
            IsAvailable = isAvailable;
            LoadRecovery = loadRecovery;
        }

        public bool IsAvailable { get; }

        public SaveRecoveryState LoadRecovery { get; }

        public PurchaseLedgerState State => Clone(state);

        public static PersistencePurchaseLedger Load(IPersistenceService persistence, SaveDefinition<PurchaseLedgerState> definition)
        {
            if (persistence == null)
                throw new ArgumentNullException(nameof(persistence));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            LoadResult<PurchaseLedgerState> result = persistence.Load(definition);
            bool available = result.Success && result.Value != null;
            return new PersistencePurchaseLedger(
                persistence,
                definition,
                available ? result.Value : new PurchaseLedgerState(),
                available,
                result.Recovery);
        }

        public PurchaseLedgerWriteResult RecordObserved(PurchaseTransactionRecord record)
        {
            if (!IsAvailable)
                return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Unavailable, PurchaseLedgerCodes.Unavailable);
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            int index = IndexOf(record.transactionId);
            if (index >= 0)
            {
                PurchaseTransactionRecord existing = state.transactions[index];
                if (IsSameIdentity(existing, record))
                    return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Duplicate, null, Clone(existing));

                return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Conflict, PurchaseLedgerCodes.TransactionConflict, Clone(existing));
            }

            PurchaseLedgerState candidate = Clone(state);
            PurchaseTransactionRecord copy = Clone(record);
            copy.state = PurchaseTransactionState.Observed;
            candidate.transactions.Add(copy);
            candidate.catalogVersion = Math.Max(candidate.catalogVersion, copy.catalogVersion);

            return SaveAndPublish(candidate, copy);
        }

        public PurchaseLedgerWriteResult UpdateState(string transactionId, PurchaseTransactionState nextState, string code = null)
        {
            if (!IsAvailable)
                return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Unavailable, PurchaseLedgerCodes.Unavailable);
            if (string.IsNullOrEmpty(transactionId))
                throw new ArgumentException("Transaction id cannot be empty.", nameof(transactionId));

            int index = IndexOf(transactionId);
            if (index < 0)
                return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.NotFound, PurchaseLedgerCodes.NotFound);

            PurchaseLedgerState candidate = Clone(state);
            PurchaseTransactionRecord record = candidate.transactions[index];
            record.state = nextState;
            record.errorCode = code;
            record.lastUpdatedUtcTicks = DateTime.UtcNow.Ticks;

            return SaveAndPublish(candidate, record);
        }

        public bool ContainsCompletedTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return false;

            foreach (PurchaseTransactionRecord record in state.transactions)
            {
                if (string.Equals(record.transactionId, transactionId, StringComparison.Ordinal) &&
                    record.state == PurchaseTransactionState.Completed)
                    return true;
            }

            return false;
        }

        private PurchaseLedgerWriteResult SaveAndPublish(PurchaseLedgerState candidate, PurchaseTransactionRecord changed)
        {
            SaveResult result = persistence.Save(definition, candidate);
            if (!result.Success)
                return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.PersistenceFailed, result.DiagnosticCode ?? PurchaseLedgerCodes.SaveFailed);

            state = candidate;
            return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Saved, null, Clone(changed));
        }

        private int IndexOf(string transactionId)
        {
            for (int i = 0; i < state.transactions.Count; i++)
            {
                if (string.Equals(state.transactions[i].transactionId, transactionId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static bool IsSameIdentity(PurchaseTransactionRecord left, PurchaseTransactionRecord right)
        {
            return string.Equals(left.productId, right.productId, StringComparison.Ordinal) &&
                   string.Equals(left.storeName, right.storeName, StringComparison.Ordinal) &&
                   string.Equals(left.storeProductId, right.storeProductId, StringComparison.Ordinal) &&
                   string.Equals(left.receiptFingerprintSha256, right.receiptFingerprintSha256, StringComparison.Ordinal);
        }

        private static PurchaseLedgerState Clone(PurchaseLedgerState source)
        {
            var copy = new PurchaseLedgerState
            {
                schemaVersion = source.schemaVersion,
                catalogVersion = source.catalogVersion
            };

            if (source.transactions != null)
            {
                foreach (PurchaseTransactionRecord record in source.transactions)
                    copy.transactions.Add(Clone(record));
            }

            return copy;
        }

        private static PurchaseTransactionRecord Clone(PurchaseTransactionRecord source)
        {
            if (source == null)
                return null;

            return new PurchaseTransactionRecord
            {
                transactionId = source.transactionId,
                productId = source.productId,
                storeName = source.storeName,
                storeProductId = source.storeProductId,
                productType = source.productType,
                source = source.source,
                state = source.state,
                firstObservedUtcTicks = source.firstObservedUtcTicks,
                lastUpdatedUtcTicks = source.lastUpdatedUtcTicks,
                receiptFingerprintSha256 = source.receiptFingerprintSha256,
                validationMetadataJson = source.validationMetadataJson,
                validationAttemptCount = source.validationAttemptCount,
                grantAttemptCount = source.grantAttemptCount,
                confirmationAttemptCount = source.confirmationAttemptCount,
                errorCode = source.errorCode,
                storeConfirmationRequired = source.storeConfirmationRequired,
                catalogVersion = source.catalogVersion
            };
        }
    }
}
