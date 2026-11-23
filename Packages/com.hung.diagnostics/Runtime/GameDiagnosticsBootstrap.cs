using UnityEngine;

namespace Hung.Diagnostics
{
    public sealed class GameDiagnosticsBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameLogConfig config;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            var go = new GameObject("[GameDiagnostics]");
            DontDestroyOnLoad(go);

            var bootstrap = go.AddComponent<GameDiagnosticsBootstrap>();

            bootstrap.config = Resources.Load<GameLogConfig>("GameLogConfig");
            GameLog.Initialize(bootstrap.config);
        }

        private void OnApplicationQuit()
        {
            GameLog.Shutdown();
        }

        private void OnDestroy()
        {
            GameLog.Shutdown();
        }
    }
}