using System;
using Hung.Base;
using Hung.DesignPattern;
using UnityEngine;

namespace Hung.LiveOps.PiggyBank
{
    public class PiggyBankManager : Singleton<PiggyBankManager>, IPiggyBankService
    {
        private void OnDestroy() => ServiceSlots.Unregister<IPiggyBankService>(this);

        void Start()
        {
            if (!Hung.Base.ServiceSlotLifecycle.CanInitialize<IPiggyBankService>(this, this)) return;

            DontDestroyOnLoad(gameObject);
            ServiceSlots.Register<IPiggyBankService>(this);
        }
        private PiggyBankSaveData _saveData;
        private PiggyBankSaveData SaveData => _saveData ??= LoadPiggyBankSaveData();
        private const string SaveDataKey = "PiggyBankSaveData";
        private const int PASS_LEVEL_MULTIPLE_PROGRESS = 100;
        public int PiggyGoldProgress => GetLevelProgress * PASS_LEVEL_MULTIPLE_PROGRESS;
        private int GetLevelProgress => SaveData.currentLevelProgress;
        private PiggyBankSaveData LoadPiggyBankSaveData()
        {
            return Database.Load<PiggyBankSaveData>();
        }

        public void IncreasePiggyProgress()
        {
            SaveData.currentLevelProgress++;
            SavePiggyBankData();
        }

        public void ResetPiggyProgress()
        {
            SaveData.currentLevelProgress = 0;
            SavePiggyBankData();
        }

        private void SavePiggyBankData()
        {
            if (_saveData == null) return;
            Database.Save(_saveData);
        }
    }
}