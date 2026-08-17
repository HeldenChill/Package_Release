using System.Collections.Generic;

namespace Hung.Data.Persistence
{
    /// <summary>
    /// Read-only enumeration of the subdirectory names <see cref="FileSaveStore"/> and
    /// <see cref="FileCanonicalEvidenceStore"/> create under a persistence root. Lets a product
    /// implement a wipe-once reset without hard-coding the layout in product code. Enumerate
    /// only - deletion policy stays product-side (plan §9.3).
    /// </summary>
    public static class OwnedRootLayout
    {
        /// <summary>
        /// The fixed set of directory names created under a persistence root: "primary",
        /// "backup", "quarantine", "receipts".
        /// </summary>
        public static IReadOnlyList<string> DirectoryNames { get; } = new[]
        {
            "primary",
            "backup",
            "quarantine",
            "receipts",
        };
    }
}
