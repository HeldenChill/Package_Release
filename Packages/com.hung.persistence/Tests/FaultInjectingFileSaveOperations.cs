using System;
using System.IO;
using Hung.Data.Persistence;

namespace Hung.Persistence.Tests
{
    /// <summary>
    /// Wraps a real <see cref="IFileSaveOperations"/> and can be told to throw at a named
    /// boundary, so D4's crash-point characterisation (gate G4) exercises the exact
    /// write/receipt ordering instead of a generic double-run.
    /// </summary>
    internal enum FaultBoundary
    {
        None,
        BeforeWrite,
        AfterTemp,
        AfterBackupCopy,
        AfterPrimaryMove,
        BeforeReceipt,
        AfterReceipt
    }

    internal sealed class FaultInjectingFileSaveOperations : IFileSaveOperations
    {
        private readonly IFileSaveOperations inner;
        public FaultBoundary FailAt = FaultBoundary.None;
        public int CreateNewCallCount;
        public int MoveCallCount;

        public FaultInjectingFileSaveOperations(IFileSaveOperations inner)
        {
            this.inner = inner;
        }

        public void CreateDirectory(string path)
        {
            if (FailAt == FaultBoundary.BeforeWrite)
                throw new IOException("Injected fault: before write.");
            inner.CreateDirectory(path);
        }

        public bool FileExists(string path) => inner.FileExists(path);
        public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);

        public Stream CreateNew(string path)
        {
            CreateNewCallCount++;
            Stream stream = inner.CreateNew(path);
            if (FailAt == FaultBoundary.AfterTemp)
            {
                stream.Dispose();
                throw new IOException("Injected fault: after temp file created.");
            }

            return stream;
        }

        public void Copy(string sourcePath, string destinationPath, bool overwrite)
        {
            inner.Copy(sourcePath, destinationPath, overwrite);
            if (FailAt == FaultBoundary.AfterBackupCopy)
                throw new IOException("Injected fault: after backup copy.");
        }

        public void Move(string sourcePath, string destinationPath)
        {
            MoveCallCount++;
            inner.Move(sourcePath, destinationPath);
            if (FailAt == FaultBoundary.AfterPrimaryMove)
                throw new IOException("Injected fault: after primary move.");
        }

        public void Delete(string path) => inner.Delete(path);
    }

    /// <summary>
    /// Fault-injecting <see cref="ICanonicalEvidenceStore"/> wrapping a real store, so the
    /// BeforeReceipt / AfterReceipt boundaries (which land inside CommitReceipt, not inside
    /// IFileSaveOperations) can be characterised too.
    /// </summary>
    internal sealed class FaultInjectingEvidenceStore : Hung.Base.Persistence.ICanonicalEvidenceStore
    {
        private readonly Hung.Base.Persistence.ICanonicalEvidenceStore inner;
        public FaultBoundary FailAt = FaultBoundary.None;

        public FaultInjectingEvidenceStore(Hung.Base.Persistence.ICanonicalEvidenceStore inner)
        {
            this.inner = inner;
        }

        public void CommitReceipt(string key, int schemaVersion, DateTime firstCommittedUtc)
        {
            if (FailAt == FaultBoundary.BeforeReceipt)
                throw new IOException("Injected fault: before receipt commit.");
            inner.CommitReceipt(key, schemaVersion, firstCommittedUtc);
            if (FailAt == FaultBoundary.AfterReceipt)
                throw new IOException("Injected fault: after receipt commit.");
        }

        public bool HasReceipt(string key) => inner.HasReceipt(key);
    }
}
