using UnityEngine;

namespace Hung.AutoTest
{
    /// <summary>
    /// Registers <see cref="AutoTestRuntimeCapability.FakeInput"/> availability at startup.
    /// </summary>
    internal static class AutoTestFakeInputRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            AutoTestCapabilityRegistry.Register(
                AutoTestRuntimeCapability.FakeInput,
                Probe);
        }

        private static AutoTestCapabilityCheck Probe()
        {
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null
                ? AutoTestCapabilityCheck.Unavailable("Fake input requires a graphics device.")
                : AutoTestCapabilityCheck.Available();
        }
    }
}
