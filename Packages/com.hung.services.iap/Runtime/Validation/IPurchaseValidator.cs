using System.Threading;
using System.Threading.Tasks;

namespace Hung.IAP
{
    public interface IPurchaseValidator
    {
        Task<PurchaseValidationResult> ValidateAsync(StorePurchaseRecord record, PurchaseCatalogEntry entry, CancellationToken token);
    }

    public enum PurchaseValidationStatus
    {
        Valid,
        Rejected,
        RetryableFailure,
        ConfigurationError
    }

    public readonly struct PurchaseValidationResult
    {
        private PurchaseValidationResult(PurchaseValidationStatus status, string code, string metadataJson)
        {
            Status = status;
            Code = code;
            MetadataJson = metadataJson;
        }

        public PurchaseValidationStatus Status { get; }
        public string Code { get; }
        public string MetadataJson { get; }

        public static PurchaseValidationResult Valid(string metadataJson) => new PurchaseValidationResult(PurchaseValidationStatus.Valid, null, metadataJson);
        public static PurchaseValidationResult Rejected(string code) => new PurchaseValidationResult(PurchaseValidationStatus.Rejected, code, null);
        public static PurchaseValidationResult RetryableFailure(string code) => new PurchaseValidationResult(PurchaseValidationStatus.RetryableFailure, code, null);
        public static PurchaseValidationResult ConfigurationError(string code) => new PurchaseValidationResult(PurchaseValidationStatus.ConfigurationError, code, null);
    }
}
