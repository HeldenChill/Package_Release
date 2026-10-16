using System;
using UnityEngine;

namespace Hung.Ads
{
    /// <summary>Opt-in current-launch authorization for ad loads, independent of vendor SDK readiness.</summary>
    public static class AdsStartupGate
    {
        /// <summary>True by default; Hold blocks loads until this launch calls Authorize.</summary>
        public static bool IsAuthorized { get; private set; } = true;
        /// <summary>Raised once when an explicit hold is authorized. Vendor gates recheck SDK readiness.</summary>
        public static event Action Authorized;

        /// <summary>Hold ad loads for the current launch; call before any scene can request loads.</summary>
        public static void Hold() => IsAuthorized = false;

        /// <summary>Allow loads after consent resolution, including contextual ads after refusal.</summary>
        public static void Authorize()
        {
            if (IsAuthorized) return;
            IsAuthorized = true;
            Authorized?.Invoke();
        }

        /// <summary>Reset per-launch state. Other games remain automatically authorized unless they opt in.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetForPlay()
        {
            IsAuthorized = true;
            Authorized = null;
        }
    }
}
