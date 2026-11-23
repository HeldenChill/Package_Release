# DailyReward — install
1. Add `Runtime/DailyRewardManager.cs`'s host GameObject (or an equivalent bootstrap component) to your boot/Home scene.
2. Create a `DailyRewardDataSO` asset and assign it on the manager, filling in the reward table.
3. Open UI via `UIManager.Ins.OpenUI<DailyRewardPopup>()`; service API via `Locator.DailyReward` (`IDailyRewardService`).
4. Demo scene: deferred — author a minimal scene here when first adopting this package (user-signed decision, Phase 5 plan).
