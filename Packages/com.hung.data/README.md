# Hung Data (L2)

Data framework: SO definitions, save, DataManager. Second assembly: Hung.Common - holds only `ConditionBarrier.cs` (its former `SStats.Stat`/`StatModifier`/`StatModType` duplicate was deleted 2026-07-11, Hung.Base is now sole owner). Kept as a separate assembly deliberately (Ph3 Task 3.3, 2026-07-11): dissolving it into Hung.Data would mean repointing 8 consumer asmdefs for zero functional gain - not worth the churn for one file.

## Item Catalog Authoring

Create game items in `ItemCatalog.asset` with stable `ItemId` values. Base IDs use the `base.` namespace; game IDs should use a game namespace, for example `pet_vs_monster.gem`.

Run item catalog validation before release. Duplicate IDs, duplicate code names, null definitions, and invalid identifiers fail validation. Rename IDs only with a save/content migration plan; IDs are serialized keys.

Run the ItemId code generator after catalog changes. It writes deterministic game constants, skipping `base.` entries. Generated constants are convenience wrappers; runtime lookup still uses catalog data.

Odin item selectors read available catalog entries for fields and dictionary keys. If an asset contains an unknown raw ID, fix the catalog or migrate the asset before shipping.

Known debt:
- Runtime/ contains a Hung.Base.asmref (asmref-injection pattern) - retirement planned per master-project vision.
