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
    public interface IGameplayService
    {
        public void ConstructLevel(int level);
        public void DestructLevel();
        public void AddMoney(int money);
        public void UsingBooster(int type);
        public void ChangeScene(int id);
        public void UsingSkill(int type, int rarity = 0);
        public int IsCanUseBooster(int type);
        public int IsCanUseSkill(int type);
        public void Revive();
        bool IsActiveScene(int sceneId);
        void SetActiveScene(int sceneId);
        void OnEndGame(bool isWin = false);
        void ShakeCamera(float duration = 2f);
        Camera GameplayCamera { get; }
        Transform PlayerTransform { get; }
    }
    #endregion
}