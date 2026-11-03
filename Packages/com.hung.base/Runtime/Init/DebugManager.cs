using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base.Init
{
    using Hung.DesignPattern;
    using Hung.UI;

    public class DebugManager : SimpleSingleton<DebugManager>
    {
        /// <summary>
        /// Raised at the end of OnInit when debug configuration has been applied.
        /// </summary>
        public static event Action Initialized;

        [SerializeField]
        protected GameObject FpsDebug;
        [SerializeField]
        protected GameObject LogDebug;
        [SerializeField]
        protected bool isShowAds = false;
        [SerializeField]
        protected bool isUseIap = false;
        List<UICanvas> DebugCanvass = new List<UICanvas>();

        protected int level = -1;
        public int Level => level;
        public bool IsShowAds => isShowAds;
        public bool IsUseIap => isUseIap;

        /// <summary>
        /// Flips whether IAP is enabled for runtime cheats and debug bypasses.
        /// </summary>
        /// <param name="value">True to enable IAP; false to disable.</param>
        public void SetUseIap(bool value) => isUseIap = value;

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

        public virtual void OnInit(bool isDebugFps, bool isDebugLog, bool isShowAds, bool isUseIap, int level = -1)
        {
            _instance = this;

            FpsDebug.SetActive(isDebugFps);
            LogDebug.SetActive(isDebugLog);
            this.isShowAds = isShowAds;
            this.isUseIap = isUseIap;
            this.level = level;

            if (level >= 0)
            {
                PersistSelectedLevel(level);
            }

            Initialized?.Invoke();
        }

        /// <summary>
        /// Writes the selected level to the persistent GameData save. Subclasses can override to redirect or omit.
        /// </summary>
        /// <param name="selectedLevel">The non-negative level index to persist.</param>
        protected virtual void PersistSelectedLevel(int selectedLevel)
        {
            GameData gameData = Database.Load<GameData>(GameData.SaveKey);
            gameData.user.normalLevelIndex = selectedLevel;
            Database.Save(gameData, GameData.SaveKey);
        }
    }
}
