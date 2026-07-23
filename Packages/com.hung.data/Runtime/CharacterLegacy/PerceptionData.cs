using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace Utilities.Core.Character
{
    public class PerceptionData 
    {
        #region PROPERTY STATS
        private Transform characterTransform;
        private Transform skinCharacterTransform;
        private bool isFaceRight = true;       
        public void Initialize(Transform characterTransform, Transform skinCharacterTransform)
        {
            this.characterTransform = characterTransform;
            perceptionStats = new PerceptionStatData();
            characteristicStats = new CharacteristicStatData();
            this.skinCharacterTransform = skinCharacterTransform;
        }
        public bool IsFaceRight { 
            get => isFaceRight; 
        }      

        public Transform Tf { 
            get => characterTransform;
        }

        public Transform SkinTf
        {
            get => skinCharacterTransform;
        }
        #endregion

        #region PERCEPTION STATS
        protected PerceptionStatData perceptionStats;
        protected CharacteristicStatData characteristicStats;
        public PerceptionStatData PerceptionStats => perceptionStats;
        public CharacteristicStatData CharacteristicStats => characteristicStats;
        #endregion
        public class PerceptionStatData
        {
            public int ThreatLevel;
            public int SurvivalLevel;
            public int StrongLevel;

            public PerceptionStatData()
            {
                ThreatLevel = 0;
                SurvivalLevel = 100;
                StrongLevel = 100;
            }
        }
        public class CharacteristicStatData
        {
            public int AggressionLevel;
            public int SelfishLevel;
            public int ConfidenceLevel;
            public int DecisiveLevel;
            public int CarefulLevel;
        }
    }
}