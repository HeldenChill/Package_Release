using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hung.DesignPattern;

namespace Hung.Ads
{
    using Hung.Base.Init;
    using Hung.Base;

    public class GameAppOpenAds : MonoBehaviour, IAds
    {
        GameConfig config;
        // Lazy: Locator.Data is not guaranteed to be set when this Awake runs
        // (this GO survives scene loads via DontDestroyOnLoad).
        GameConfig Config => config ??= Locator.Data?.GetSOData<GameConfig>();
        bool isCanShow = true;
        EventBinding<ResetAoaCapEvent> _resetAoaBinding;

        GameData gameData;
        GameData GameData => gameData ??= Locator.Data.GetData<GameData>();

        public ADS_TYPE Type { get; set; }

        private void Awake()
        {
            isCanShow = true;
            _resetAoaBinding = new EventBinding<ResetAoaCapEvent>(OnResetCapping);
            EventBus<ResetAoaCapEvent>.Subscribe(_resetAoaBinding);
        }

        private void OnDestroy()
        {
            EventBus<ResetAoaCapEvent>.Unsubscribe(_resetAoaBinding);
        }
        public void Hide()
        {
            
        }

        public void Load()
        {
            
        }
        public void Show()
        {
            if (Config == null) return;

            if(!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if(GameData.user.normalLevelIndex >= Config.StartInterLevel)
                {
                    if (isCanShow)
                    {
                        
                    }
                    else
                    {
                        isCanShow = true;
                    }
                }
            }
        }

        private void OnResetCapping()
        {
            isCanShow = false;
        }
    }
}