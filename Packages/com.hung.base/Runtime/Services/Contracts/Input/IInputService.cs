namespace Hung.Base
{
    /// <summary>Input service contract. Game-specific surface (e.g. generated
    /// input-action accessors) is added by the consuming game via its
    /// Hung.Base asmref folder as another partial part.</summary>
    public partial interface IInputService
    {
        /// <summary>Enable or disable the named action maps.</summary>
        void SetInput(bool active, params string[] maps);

        /// <summary>Disable every action map.</summary>
        void DisableAllInput();
    }
}
