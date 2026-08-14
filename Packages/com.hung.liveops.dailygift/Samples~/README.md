# DailyGift — install
1. Add `Runtime/DailyGiftManager.prefab` to your boot/Home scene.
2. Assign a game-owned `ScriptableObject` implementing `Hung.Data.LiveOps.IDailyGiftConfig` on manager and popup. PVM's existing `DailyGiftDataSO` is intended adapter; preserve its serialized fields and script GUID.
3. Open UI via `UIManager.Ins.OpenUI<DailyGiftPopup>()`; service API via `Locator.DailyGift` (`IDailyGiftService`).
4. Demo scene: deferred — author a minimal scene here when first adopting this package (user-signed decision, Phase 5 plan).
