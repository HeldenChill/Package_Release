namespace Hung.UI.Scoping
{
    /// <summary>
    /// What <see cref="UiScopeRegistry"/> needs from a canvas. Kept to two members so the
    /// registry can be exercised without a UIManager, a Locator, or a live scene.
    /// </summary>
    public interface IScopedUi
    {
        UiScope Scope { get; }

        /// <summary>Called when the live path leaves this canvas's bound scope.</summary>
        void OnScopeExit();
    }
}
