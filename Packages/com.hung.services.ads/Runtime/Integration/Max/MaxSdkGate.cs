using System;
using UnityEngine;

namespace Hung.Ads.Integration.Max
{
    /// <summary>Holds MAX loads until both current-launch authorization and SDK readiness are true.</summary>
    internal static class MaxSdkGate
    {
        static ReadyGate _gate;
        static bool _subscribed;
        internal static int SubmittedLoads { get; private set; }
        internal static int Waiting => _gate == null ? 0 : _gate.Waiting;

        internal static void Run(Action load)
        {
            if (_gate == null) _gate = new ReadyGate(() => AdsStartupGate.IsAuthorized && MaxSdk.IsInitialized());
            if (!_subscribed)
            {
                MaxSdkCallbacks.OnSdkInitializedEvent += OnInitialized;
                AdsStartupGate.Authorized += OnAuthorized;
                _subscribed = true;
            }
            _gate.Run(() => { SubmittedLoads++; load(); });
        }

        static void OnInitialized(MaxSdkBase.SdkConfiguration configuration) => _gate?.Release();
        static void OnAuthorized() => _gate?.Release();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay()
        {
            if (_subscribed)
            {
                MaxSdkCallbacks.OnSdkInitializedEvent -= OnInitialized;
                AdsStartupGate.Authorized -= OnAuthorized;
            }
            _subscribed = false;
            _gate = null;
            SubmittedLoads = 0;
        }
    }
}
