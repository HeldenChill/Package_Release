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
            // LoadStart auto-advances to GameScene. Loaded BY NAME, not build index: CryptoLoader
            // is a debug-only gate that can be unchecked in Build Settings for a release-style
            // run, which shifts every following scene's index down by one. A hardcoded index 2
            // then points past the end of the list ("Scene with build index: 2 couldn't be
            // loaded") or at the wrong scene. Name lookup is immune to index shifts either way.
            Hung.Base.SceneGameManager.Ins.LoadingSceneAsync("GameScene");
        }
    }
}