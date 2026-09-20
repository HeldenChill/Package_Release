using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace Gameplay.Character
{
    public class PerceptionData 
    {
        #region PROPERTY STATS
        private Transform characterTf;
        private Transform skinCharacterTf;
        private Transform headCharacterTf;
        private Transform footCharacterTf;
        private Transform cameraTf;
        private bool isFaceRight = true; 
        private ICharacter character;      
        public PerceptionData Initialize(Transform characterTransform, Transform skinCharacterTransform)
        {
            this.characterTf = characterTransform;
            perceptionStats = new PerceptionStatData();
            characteristicStats = new CharacteristicStatData();
            this.skinCharacterTf = skinCharacterTransform;
            return this;
        }
        public PerceptionData SetHeadTf(Transform headTf)
        {
            headCharacterTf = headTf;
            return this;
        }
        public PerceptionData SetFootTf(Transform footTf)
        {
            footCharacterTf = footTf;
            return this;
        }
        public PerceptionData SetCameraTf(Transform cameraTf)
        {
            this.cameraTf = cameraTf;
            return this;
        }
        public PerceptionData SetCharacter(ICharacter character)
        {
            this.character = character;
            return this;
        }
        public bool IsFaceRight { 
            get => isFaceRight; 
        }      

        public Transform Tf => characterTf;
        public Transform SkinTf => skinCharacterTf;
        public Transform HeadTf => headCharacterTf;
        public Transform CameraTf => cameraTf;
        public Transform FootTf => footCharacterTf;
        public ICharacter Character => character;
        public bool IsTeleporting = false;
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