# Hung Base (L1)

Core runtime: Locator + service contracts (LocatorServices), init flow (InitManager/LoadStart), Stats, GameData, base app glue. Second assembly: Hung.Utilities.Input.

## ItemId Runtime Contract

Item identity is `ItemId`: a serialized, namespaced value such as `base.gold` or `pet_vs_monster.gem`. Runtime save data and item service APIs use `ItemId`; the removed numeric item enum is no longer part of the contract.

`GameData.SaveKey` is `GameData.item-id-v1`. Call `InitData(IEnumerable<ItemId>)` with catalog IDs during startup. Existing unknown saved IDs are preserved; missing catalog IDs are added without deleting saved entries.

Use `Locator.Items.GetPresentation(id)` for icons, display names, and rarity. Do not read metadata from `GameplayData.Items`; that legacy catalog has been removed.

Known debt (carried from extraction, see Docs/audit/canon-decisions.md):
- Hung.Base.asmdef references spine-unity, Unity.TextMeshPro, Unity.InputSystem - vendor refs in L1; spine removal planned Phase 1b.
