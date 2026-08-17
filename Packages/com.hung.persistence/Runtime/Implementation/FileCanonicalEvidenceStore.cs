using System;
using System.Globalization;
using System.IO;
using Hung.Base.Persistence;

namespace Hung.Data.Persistence
{
    /// <summary>
    /// File-backed <see cref="ICanonicalEvidenceStore"/>. Writes one receipt per key to
    /// <c>&lt;root&gt;/receipts/&lt;key&gt;.receipt</c> using the same temp-write-then-move
    /// discipline as <see cref="FileSaveStore.Write"/>. A receipt records that a key completed
    /// a canonical commit; it carries no payload bytes and no secret material.
    /// </summary>
    public sealed class FileCanonicalEvidenceStore : ICanonicalEvidenceStore
    {
        private const string CompletedMarker = "IMPORT_COMPLETED";

        private readonly string receiptsDirectory;
        private readonly IFileSaveOperations operations;

        public FileCanonicalEvidenceStore(string root, IFileSaveOperations operations = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            this.operations = operations ?? new SystemFileSaveOperations();
            receiptsDirectory = Path.Combine(root, "receipts");
        }

        public void CommitReceipt(string key, int schemaVersion, DateTime firstCommittedUtc)
        {
            string validKey = SaveDefinition.ValidateKey(key);
            string path = ReceiptPath(validKey);
            operations.CreateDirectory(receiptsDirectory);
            string temp = Path.Combine(receiptsDirectory, validKey + "." + Guid.NewGuid().ToString("N") + ".tmp");
            string content = validKey + "\n" + schemaVersion.ToString(CultureInfo.InvariantCulture) + "\n"
                + firstCommittedUtc.ToString("O", CultureInfo.InvariantCulture) + "\n" + CompletedMarker;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            using (Stream stream = operations.CreateNew(temp))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }

            if (operations.FileExists(path))
                operations.Delete(path);
            operations.Move(temp, path);
        }

        public bool HasReceipt(string key)
        {
            string validKey = SaveDefinition.ValidateKey(key);
            return operations.FileExists(ReceiptPath(validKey));
        }

        private string ReceiptPath(string validKey) => Path.Combine(receiptsDirectory, validKey + ".receipt");
    }
}
