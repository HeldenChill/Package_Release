using System;
using Hung.Base.Persistence;

namespace Hung.IAP
{
    public static class PurchaseLedgerDefinition
    {
        public const string Key = "purchase-ledger";
        public const int SchemaVersion = 1;

        public static SaveDefinition<PurchaseLedgerState> Create(ISaveCodec codec, ISaveProtector protector)
        {
            return new SaveDefinition<PurchaseLedgerState>(
                Key,
                SchemaVersion,
                CreateDefault,
                Validate,
                Array.Empty<ISaveMigration>(),
                Array.Empty<string>(),
                codec,
                protector,
                SaveFailurePolicy.FailClosed);
        }

        private static PurchaseLedgerState CreateDefault() => new PurchaseLedgerState();

        private static SaveValidationResult Validate(PurchaseLedgerState state)
        {
            if (state == null || state.transactions == null)
                return SaveValidationResult.Invalid("PURCHASE_LEDGER_INVALID");
            if (state.schemaVersion > SchemaVersion)
                return SaveValidationResult.Invalid("PURCHASE_LEDGER_NEWER_SCHEMA");

            return SaveValidationResult.Valid();
        }
    }
}
