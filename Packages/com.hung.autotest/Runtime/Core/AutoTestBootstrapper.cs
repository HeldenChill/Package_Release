using System.Collections;
using UnityEngine;

namespace Hung.AutoTest
{
    /// <summary>
    /// Boots the game from any startup scene (CryptoLoader init screen, LoadStart)
    /// into GameScene so AutoTest can run with one press. No-op when gameplay is
    /// already loaded.
    /// </summary>
    public static class AutoTestBootstrapper
    {
        private static bool bootKicked;

        /// <summary>
        /// Extra game-specific readiness condition ANDed with the Locator checks.
        /// Set by game glue (PvM: GameplayManager.Exists — Exists, not Ins, because
        /// touching Ins from a boot scene auto-creates an empty manager with no data).
        /// Keeps this core file free of composition-root types.
        /// </summary>
        public static System.Func<bool> ExtraReadyCheck = () => true;

        /// <summary>
        /// Game-specific one-shot boot action: find the game's init/start screen and
        /// kick it (e.g. PvM: InitManager.StartGameFromAutomation). Return true if a
        /// boot was started. Set by game glue; null = nothing to kick.
        /// </summary>
        public static System.Func<bool> BootKick;

        public static bool IsGameplayReady
        {
            // The host game supplies its own readiness condition through this seam.
            get
            {
                return ExtraReadyCheck();
            }
        }

        /// <summary>
        /// One-shot boot kick. Returns true if a boot was started or gameplay is ready.
        /// Safe to call every frame (CLI editor-update loop).
        /// </summary>
        public static bool TryKickBoot()
        {
            if (IsGameplayReady)
                return true;

            if (bootKicked)
                return false;

            if (BootKick != null && BootKick())
            {
                bootKicked = true;
                return true;
            }

            // No game boot hook fired — a LoadStart-style scene chains onward by itself.
            return false;
        }

        /// <summary>Coroutine version for the runtime runner: kick + wait until gameplay is loaded.</summary>
        public static IEnumerator EnsureGameBooted(float timeoutSeconds)
        {
            if (IsGameplayReady)
                yield break;

            TryKickBoot();

            float deadline = Time.realtimeSinceStartup + Mathf.Max(5f, timeoutSeconds);
            while (!IsGameplayReady && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!IsGameplayReady)
                Debug.LogError("[AutoTestBootstrapper] Gameplay not ready after " + timeoutSeconds + "s. Scene flow: init Start button -> LoadStart -> GameScene.");
        }

        public static void ResetKick()
        {
            bootKicked = false;
        }
    }
}
