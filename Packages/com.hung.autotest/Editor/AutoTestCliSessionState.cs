#if UNITY_EDITOR
using UnityEditor;

namespace Hung.AutoTest.Editor
{
    internal sealed class AutoTestCliLaunch
    {
        public AutoTestCliLaunch(string suitePath, string requiredGameplayScene, float readyTimeoutSeconds)
        {
            SuitePath = suitePath;
            RequiredGameplayScene = requiredGameplayScene;
            ReadyTimeoutSeconds = readyTimeoutSeconds;
        }

        public string SuitePath { get; }
        public string RequiredGameplayScene { get; }
        public float ReadyTimeoutSeconds { get; }
    }

    internal static class AutoTestCliSessionState
    {
        private const string DefaultKeyPrefix = "Hung.AutoTest.Cli.";
        private const string PendingKey = "Pending";
        private const string SuitePathKey = "SuitePath";
        private const string RequiredGameplaySceneKey = "RequiredGameplayScene";
        private const string ReadyTimeoutSecondsKey = "ReadyTimeoutSeconds";

        internal static string KeyPrefixForTests { private get; set; }

        private static string Prefix
        {
            get { return string.IsNullOrEmpty(KeyPrefixForTests) ? DefaultKeyPrefix : KeyPrefixForTests; }
        }

        internal static void Begin(string suitePath, string requiredGameplayScene, float readyTimeoutSeconds)
        {
            SessionState.SetString(Prefix + SuitePathKey, suitePath ?? string.Empty);
            SessionState.SetString(Prefix + RequiredGameplaySceneKey, requiredGameplayScene ?? string.Empty);
            SessionState.SetFloat(Prefix + ReadyTimeoutSecondsKey, readyTimeoutSeconds);
            SessionState.SetBool(Prefix + PendingKey, true);
        }

        internal static bool TryClaim(out AutoTestCliLaunch launch)
        {
            string prefix = Prefix;
            if (!SessionState.GetBool(prefix + PendingKey, false))
            {
                launch = null;
                return false;
            }

            string suitePath = SessionState.GetString(prefix + SuitePathKey, string.Empty);
            string requiredGameplayScene = SessionState.GetString(prefix + RequiredGameplaySceneKey, string.Empty);
            float readyTimeoutSeconds = SessionState.GetFloat(prefix + ReadyTimeoutSecondsKey, 60f);
            Clear();
            launch = new AutoTestCliLaunch(suitePath, requiredGameplayScene, readyTimeoutSeconds);
            return true;
        }

        internal static void Clear()
        {
            string prefix = Prefix;
            SessionState.EraseBool(prefix + PendingKey);
            SessionState.EraseString(prefix + SuitePathKey);
            SessionState.EraseString(prefix + RequiredGameplaySceneKey);
            SessionState.EraseFloat(prefix + ReadyTimeoutSecondsKey);
        }
    }
}
#endif
