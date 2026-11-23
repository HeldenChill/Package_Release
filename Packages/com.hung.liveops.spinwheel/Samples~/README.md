# SpinWheel — install
1. Add `Runtime/SpinWheelManager.cs`'s host GameObject to your boot/Home scene.
2. Create a `SpinWheelDataSO` asset and assign it on the manager.
3. Open UI via `UIManager.Ins.OpenUI<SpinWheelPopup>()` (or `Locator.UI.OpenUI<SpinWheelPopup>()`); service API via `Locator.SpinWheel` (`ISpinWheelService`).
4. Demo scene: deferred — author a minimal scene here when first adopting this package (user-signed decision, Phase 5 plan).
