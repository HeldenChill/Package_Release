using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.Base.Init
{
    using Hung.DesignPattern;
    using Hung.UI;

    public class DebugManager : SimpleSingleton<DebugManager>
    {
        [SerializeField]
        GameObject FpsDebug;
        [SerializeField]
        GameObject LogDebug;
        [SerializeField]
        bool isShowAds = false;
        [SerializeField]
        bool isUseIap = false;
        List<UICanvas> DebugCanvass = new List<UICanvas>();

        int level = -1;
        public int Level => level;
        public bool IsShowAds => isShowAds;
        public bool IsUseIap => isUseIap;
        //public void OpenDebugCanvas(UI_POSITION position)
        //{
        //    if(position == UI_POSITION.NONE) return;
        //    for (int i = 0; i < DebugCanvass.Count; i++)
        //    {
        //        DebugCanvass[i].Close();
        //    }
        //    UICanvas canvas;       
        //    switch (position)
        //    {
        //        case UI_POSITION.MAIN_MENU:
        //            break;
        //        case UI_POSITION.IN_GAME:
        //            break;
        //    }

        //}
        public void OnInit(bool isDebugFps, bool isDebugLog, bool isShowAds, bool isUseIap, int level = -1)
        {
            _instance = this;

            FpsDebug.SetActive(isDebugFps);
            LogDebug.SetActive(isDebugLog);
            this.isShowAds = isShowAds;
            this.isUseIap = isUseIap;
            this.level = level;
            // Save the level to database
            GameData gameData = Database.Load<GameData>(GameData.SaveKey);
            gameData.user.normalLevelIndex = level;
            Database.Save(gameData, GameData.SaveKey);
        }
    }
}
