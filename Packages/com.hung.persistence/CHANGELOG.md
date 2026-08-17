# Changelog

All notable changes to `com.hung.persistence` are documented in this file.

## [0.1.0] - 2026-08-17

### Added

- Initial extraction of generic persistence from `com.hung.base` and `com.hung.data`.
  Behaviour-neutral: no persisted-byte change, no public signature change, no namespace change.
- Contracts moved from `com.hung.base/Runtime/Persistence/`: `IPersistenceService`,
  `ISaveStore`, `ISaveDiagnostics`, `PersistenceResults`, `SaveDefinition`, `SaveTransforms`.
- Implementation moved from `com.hung.data/Runtime/Persistence/`: `PersistenceService`,
  `FileSaveStore`, `IFileSaveOperations`, `SaveEnvelope`, `SaveMigrationRegistry`,
  `SaveCodecs`, `SaveProtectors`, `LocalSecretKeyProvider`.
- `ILegacySaveSource` split out of `com.hung.data`'s `PlayerPrefsLegacySaveSource.cs` into its
  own contract file. The PlayerPrefs implementation stays in `com.hung.data`.

- `ICanonicalEvidenceStore`, `FileCanonicalEvidenceStore`: canonical-existence receipts for
  crash-safe legacy-import gating (D4).
- `PersistenceBuilder`: fluent composition entry point (D5).
- `OwnedRootLayout`: read-only enumeration of the owned root's subdirectories, so a product can
  implement a single wipe-once reset without hard-coding the store layout. Enumerate only; no
  delete API.
- Test suite: 6 mechanism tests moved from `com.hung.data` (`PersistenceServiceTests`,
  `FileSaveStoreTests`, `SaveEnvelopeTests`, `SaveMigrationRegistryTests`, `SaveTransformTests`,
  `PersistenceBenchmarkTests`); helpers `InMemorySaveStore`/`PersistenceTestDoubles` were
  duplicated rather than moved and stay in both packages. Product-free replacement coverage added for validation rejection
  and end-to-end migration chains; crash-safety, quarantine, HMAC-mismatch, and unknown-newer-schema
  coverage already existed from D3-D6.

### Notes

- The assembly references nothing and calls no Unity API. Paths and key material are injected.
- Product save definitions deliberately remain in `com.hung.data`.
