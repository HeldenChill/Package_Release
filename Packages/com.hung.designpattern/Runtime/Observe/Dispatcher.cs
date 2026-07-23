using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern
{
    public abstract class Dispatcher<T> : Singleton<T> where T : MonoBehaviour
    {
        private readonly Dictionary<EventID, Action> _listenerEventDictionary = new();
        private readonly Dictionary<EventID, Action<object>> _listenerEventDictionaryParam = new();
        public void RegisterListenerEvent(EventID eventID, Action callback)
        {
            if (_listenerEventDictionary.ContainsKey(eventID))
                _listenerEventDictionary[eventID] += callback;
            else
                _listenerEventDictionary.Add(eventID, callback);
        }

        public void RegisterListenerEvent(EventID eventID, Action<object> callback)
        {
            if (_listenerEventDictionaryParam.ContainsKey(eventID))
                _listenerEventDictionaryParam[eventID] += callback;
            else
                _listenerEventDictionaryParam.Add(eventID, callback);
        }

        public void UnregisterListenerEvent(EventID eventID, Action callback)
        {
            if (_listenerEventDictionary.ContainsKey(eventID))
                _listenerEventDictionary[eventID] -= callback;
            else
                Debug.LogWarning("EventID " + eventID + " not found");
        }

        public void UnregisterListenerEvent(EventID eventID, Action<object> callback)
        {
            if (_listenerEventDictionaryParam.ContainsKey(eventID))
                _listenerEventDictionaryParam[eventID] -= callback;
            else
                Debug.LogWarning("EventID " + eventID + " not found");
        }

        public void PostEvent(EventID eventID)
        {
            if (_listenerEventDictionary.TryGetValue(eventID, out Action value))
                value?.Invoke();
            else
                Debug.LogWarning("EventID " + eventID + " not found");
        }
        
        public void PostEvent(EventID eventID, object param)
        {
            if (_listenerEventDictionaryParam.TryGetValue(eventID, out Action<object> value))
                value.Invoke(param);
            else
                Debug.LogWarning("EventID " + eventID + " not found");
        }

        public void ClearAllListenerEvent()
        {
            _listenerEventDictionary.Clear();
        }
    }

    public enum EventID
    {
        //NOTE: Gameplay

        //NOTE: System
        ON_DESPAWN_LEVEL = 1000,
        LEVEL_END = 1001,
        ON_ACTIVE_HOME_SCENE = 1002,
        RECONSTUCT_LEVEL = 1003,
        REVIVE = 1004,

        RESET_AOA_CAPPING = 10000,
        RESET_INTER_CAPPING = 10001,
    }
}
