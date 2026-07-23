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

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            debugCanvas._OnToggleValueChange += OnSetDebug;
            debugCanvas._OnStartGame += OnStartGame;
            debugCanvas.SetData(isDebugGridLogic, isDebugFps, isDebugLog, isShowAds, isUseIap);
        }

        private void OnSetDebug(int id, bool value)
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

        private void OnStartGame()
        {
            if (isDebug)
            {
                debugManager = Instantiate(debugObject).GetComponent<DebugManager>();
                debugManager.OnInit(isDebugFps, isDebugLog, isShowAds, isUseIap, debugCanvas.StartLevel);
                DontDestroyOnLoad(debugManager.gameObject);
            }
            SceneGameManager.Ins.LoadingSceneAsync(1);
        }
    }
}