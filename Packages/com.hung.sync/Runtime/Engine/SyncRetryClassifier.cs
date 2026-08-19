using System;

namespace Hung.Sync
{
    /// <summary>
    /// Pure, side-effect-free mapping from a transport response to a result category, and the
    /// queue-removal policy for each category. Kept free of state and I/O so its correctness is
    /// provable by unit test without a transport.
    /// </summary>
    public static class SyncRetryClassifier
    {
        /// <summary>
        /// Classifies one transport response. A delivered response preserves the authority's own
        /// decision; an undelivered one is categorized by transport outcome.
        /// </summary>
        public static SyncResultKind Classify(SyncTransportResponse response)
        {
            switch (response.Outcome)
            {
                case SyncTransportOutcome.Delivered:
                    return response.Result.Kind;
                case SyncTransportOutcome.NetworkUnavailable:
                case SyncTransportOutcome.Timeout:
                    return SyncResultKind.RetryableTransportFailure;
                case SyncTransportOutcome.AuthExpired:
                    return SyncResultKind.AuthenticationRequired;
                case SyncTransportOutcome.ProtocolError:
                    return SyncResultKind.PermanentProtocolFailure;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(response), $"Unhandled transport outcome '{response.Outcome}'.");
            }
        }

        /// <summary>
        /// Whether this category ends the operation's lifecycle and removes its pending record.
        /// Retryable, authentication-required, and conflict categories are not terminal: their
        /// records survive so the same operation id can be retried or reconciled.
        /// </summary>
        public static bool IsTerminal(SyncResultKind kind)
        {
            switch (kind)
            {
                case SyncResultKind.Accepted:
                case SyncResultKind.DuplicateAccepted:
                case SyncResultKind.RejectedBusinessRule:
                case SyncResultKind.PermanentProtocolFailure:
                    return true;
                default:
                    return false;
            }
        }
    }
}
