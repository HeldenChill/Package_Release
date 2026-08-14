using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.Base
{
    public class HandData
    {
        public Vector3 Pos1;
        public Vector3 Pos2;
        public POSITION_TYPE PosType;
    }

    public interface ITutorialService
    {
        Func<int, HandData> GetHandInfo { get; set; }
        int CurrentStep { get; }
        int CurrentStepLevel { get; }
        bool IsRunning { get; }

        void Init();
        void Run(int level);
        void Stop();
        bool IsHaveTutorial(int level);

        void AddCustomAction(string key, Action action);
        void RemoveCustomAction(string key);
        void AddCustomCondition(string key, Func<bool> func);
        void RemoveCustomCondition(string key);

        void SetCalloutTarget(Transform rect, Image frame, Vector3 worldPos, POSITION_TYPE posType);
    }
}
