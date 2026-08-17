using System;

namespace Hung.Base.Persistence
{
    public enum SaveFailurePolicy
    {
        FailClosed,
        CreateDefaultAfterEvidencePreserved
    }

    public enum SaveDataSource
    {
        None,
        Primary,
        Backup,
        LegacyPlayerPrefs,
        Default
    }

    public enum SaveRecoveryState
    {
        None,
        DefaultCreated,
        Migrated,
        BackupRestored,
        PrimaryQuarantined,
        UnsupportedNewerVersion,
        Unrecoverable
    }

    public readonly struct SaveStoreReadResult
    {
        public SaveStoreReadResult(bool success, bool exists, ReadOnlyMemory<byte> content, string errorCode = null)
        {
            Success = success;
            Exists = exists;
            Content = content;
            ErrorCode = errorCode;
        }

        public bool Success { get; }
        public bool Exists { get; }
        public ReadOnlyMemory<byte> Content { get; }
        public string ErrorCode { get; }

        public static SaveStoreReadResult Missing() => new SaveStoreReadResult(true, false, default);
        public static SaveStoreReadResult Found(byte[] content) => new SaveStoreReadResult(true, true, content ?? Array.Empty<byte>());
        public static SaveStoreReadResult Failed(string errorCode) => new SaveStoreReadResult(false, false, default, errorCode);
    }

    public readonly struct SaveStoreWriteResult
    {
        public SaveStoreWriteResult(bool success, string path = null, string errorCode = null)
        {
            Success = success;
            Path = path;
            ErrorCode = errorCode;
        }

        public bool Success { get; }
        public string Path { get; }
        public string ErrorCode { get; }

        public static SaveStoreWriteResult Succeeded(string path = null) => new SaveStoreWriteResult(true, path);
        public static SaveStoreWriteResult Failed(string errorCode) => new SaveStoreWriteResult(false, null, errorCode);
    }

    public readonly struct SaveResult
    {
        public SaveResult(bool success, string diagnosticCode = null, string storePath = null)
        {
            Success = success;
            DiagnosticCode = diagnosticCode;
            StorePath = storePath;
        }

        public bool Success { get; }
        public string DiagnosticCode { get; }
        public string StorePath { get; }
    }

    public readonly struct LoadResult<T>
    {
        public LoadResult(bool success, T value, string diagnosticCode, SaveDataSource source, SaveRecoveryState recovery, string evidencePath = null)
        {
            Success = success;
            Value = value;
            DiagnosticCode = diagnosticCode;
            Source = source;
            Recovery = recovery;
            EvidencePath = evidencePath;
        }

        public bool Success { get; }
        public T Value { get; }
        public string DiagnosticCode { get; }
        public SaveDataSource Source { get; }
        public SaveRecoveryState Recovery { get; }
        public string EvidencePath { get; }
    }

    public readonly struct SaveDiagnostic
    {
        public SaveDiagnostic(string code, string saveKey, SaveDataSource source, SaveRecoveryState recovery, string evidencePath = null, string message = null)
        {
            Code = code;
            SaveKey = saveKey;
            Source = source;
            Recovery = recovery;
            EvidencePath = evidencePath;
            Message = message;
        }

        public string Code { get; }
        public string SaveKey { get; }
        public SaveDataSource Source { get; }
        public SaveRecoveryState Recovery { get; }
        public string EvidencePath { get; }
        public string Message { get; }
    }

    public sealed class PersistenceException : Exception
    {
        public PersistenceException(string diagnosticCode)
            : base(diagnosticCode)
        {
            DiagnosticCode = diagnosticCode;
        }

        public string DiagnosticCode { get; }
    }
}
