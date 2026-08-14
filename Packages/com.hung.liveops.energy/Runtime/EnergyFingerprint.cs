using System.Text;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// FNV-1a 64-bit hash over a canonical command string, used to fingerprint command
    /// payloads for idempotent-replay detection. Deterministic across processes/domain
    /// reloads (unlike <see cref="object.GetHashCode"/> or <see cref="string.GetHashCode"/>).
    /// </summary>
    internal static class EnergyFingerprint
    {
        private const ulong OffsetBasis = 14695981039346656037;
        private const ulong Prime = 1099511628211;

        public static string Compute(string canonical)
        {
            ulong hash = OffsetBasis;
            byte[] bytes = Encoding.UTF8.GetBytes(canonical);
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= Prime;
            }
            return hash.ToString("x16");
        }
    }
}
