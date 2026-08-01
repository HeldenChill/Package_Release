using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Ins
        {
            get
            {
                // Unity's overloaded == is required here: a destroyed instance is "fake-null".
                // `is not null` / `??` bypass it and would hand back the dead object forever.
                if (_instance != null) return _instance;

                T found = FindObjectOfType<T>();
                _instance = found != null ? found : new GameObject(typeof(T).Name).AddComponent<T>();
                return _instance;
            }
        }
    }
}