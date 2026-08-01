using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utilities
{
    public enum DevId
    {
        System,
        Hung,
        Hoang,
        Gameplay,
    }
    public static class DevLog
    {
        private static List<string> DevColors = new List<string>() { "#fffd74","#6fff59", "#fa4b4b", "#6fff59",};

        // public static void Log(DevId devId, object obj)
        // {
        //     Log(devId, obj.ToString());
        // }

        public static void Log(DevId devId, string log)
        {
            Debug.Log($"<color={DevColors[(int)devId]}>[{devId}] {log}</color>");
        }
    }
}