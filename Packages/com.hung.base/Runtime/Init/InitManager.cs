//using _Game.Utilities.Grid;
using UnityEngine;

namespace Hung.Base.Init
{
    using Hung.DesignPattern;

    [DefaultExecutionOrder(-100)]
    public class InitManager : Singleton<InitManager>
    {
        #region PROPERTYS
        [SerializeField]
        bool isDebug = false;
        [SerializeField]
        bool isDebugGridLogic = false;
        [SerializeField]
        bool isDebugFps = false;
        [SerializeField]
        bool isDebugLog = false;
        [SerializeField]
        bool isShowAds = true;
        [SerializeField]
        bool isUseIap = true;
        [SerializeField]
        GameObject debugObject;
        [SerializeField]
        InitCanvas debugCanvas;
        DebugManager debugManager;
        #endregion

        /// <summary>
        /// True when the game was booted or started through automation in this session.
        /// </summary>
        public static bool StartedFromAutomation { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAutomationFlag() => StartedFromAutomation = false;

        protected virtual void Awake()
        {
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (debugCanvas != null)
            {
                debugCanvas._OnToggleValueChange += OnSetDebug;
                debugCanvas._OnStartGame += OnStartGame;
                debugCanvas.SetData(isDebugGridLogic, isDebugFps, isDebugLog, isShowAds, isUseIap);
            }
        }

        protected virtual void OnDestroy()
        {
            if (debugCanvas != null)
            {
                debugCanvas._OnToggleValueChange -= OnSetDebug;
                debugCanvas._OnStartGame -= OnStartGame;
            }
        }

        protected virtual void OnSetDebug(int id, bool value)
        {
            switch (id)
            {
                case 0:
                    isDebugGridLogic = value;
                    break;
                case 1:
                    isDebugFps = value;
                    break;
                case 2:
                    isDebugLog = value;
                    break;
                case 3:
                    isShowAds = value;
                    break;
                case 4:
                    isUseIap = value;
                    break;
            }
        }

        /// <summary>
        /// Dev tooling seam (AutoTest/TestScenarioMode). Sets the automation flag and triggers the game start flow.
        /// </summary>
        public void StartGameFromAutomation()
        {
            StartedFromAutomation = true;
            OnStartGame();
        }

        protected virtual void OnStartGame()
        {
            if (isDebug)
            {
                debugManager = Instantiate(debugObject).GetComponent<DebugManager>();
                if (debugCanvas != null)
                {
                    debugManager.OnInit(isDebugFps, isDebugLog, isShowAds, isUseIap, debugCanvas.StartLevel);
                }
                else
                {
                    debugManager.OnInit(isDebugFps, isDebugLog, isShowAds, isUseIap);
                }

                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(debugManager.gameObject);
                }
            }
            LoadNextScene();
        }

        /// <summary>
        /// Loads the initial gameplay or loading scene. Subclasses can override to customize scene loading.
        /// </summary>
        protected virtual void LoadNextScene()
        {
            SceneGameManager.Ins.LoadingSceneAsync(1);
        }
    }
}
