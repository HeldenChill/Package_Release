using System;
using System.Collections;
using System.Collections.Generic;
using Hung.UI;
using Hung.DesignPattern;
using UnityEngine;
namespace Hung.Base.Init
{
    public class LoadStart : Singleton<LoadStart>
    {
        void Start()
        {
            // LoadStart auto-advances to GameScene (build index 2); progress drives the bar via _OnLoadingProgress
            Hung.Base.SceneGameManager.Ins.LoadingSceneAsync(2);
        }
    }
}