# PiggyBank — install
1. Add `Runtime/PiggyBankManager.cs`'s host GameObject to your boot/Home scene.
2. Create a `PiggyBankDataSO` asset and assign it on the manager.
3. Open UI via `UIManager.Ins.OpenUI<PiggyBankPopup>()` (or `Locator.UI.OpenUI<PiggyBankPopup>()`); service API via `Locator.PiggyBank` (`IPiggyBankService`).
4. Subscribe to `EventBus<PiggyBankRewardClaimedEvent>` if your home screen needs to react to a claim (e.g. fly-to-icon reward animation).
5. Demo scene: deferred — author a minimal scene here when first adopting this package (user-signed decision, Phase 5 plan).
