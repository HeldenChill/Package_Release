using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    public interface IProgress
    {
        public Action<float> _OnLoadingProgress { get; set; }
    }
    #region GAME
    
    public interface ILevelService
    {
        public T GetLevelData<T>(int level = 0) where T : class;
    }
    #endregion
}