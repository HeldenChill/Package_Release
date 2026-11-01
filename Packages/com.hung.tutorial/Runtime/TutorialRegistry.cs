using System;
using System.Collections.Generic;
using Hung.Base;
using Utilities;

namespace Hung.Tutorial
{
    public class TutorialRegistry
    {
        private readonly Dictionary<string, Action> _actions = new Dictionary<string, Action>();
        private readonly Dictionary<string, Func<bool>> _conditions = new Dictionary<string, Func<bool>>();

        public void AddAction(string key, Action action)
        {
            if (string.IsNullOrEmpty(key)) return;
            _actions[key] = action;
        }

        public void RemoveAction(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _actions.Remove(key);
        }

        public void InvokeAction(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_actions.TryGetValue(key, out var action))
            {
                action?.Invoke();
            }
            else
            {
                DevLog.Log(DevId.System, $"[TutorialRegistry] No action registered for key: {key}");
            }
        }

        public void AddCondition(string key, Func<bool> func)
        {
            if (string.IsNullOrEmpty(key)) return;
            _conditions[key] = func;
        }

        public void RemoveCondition(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _conditions.Remove(key);
        }

        public bool EvaluateCondition(string key)
        {
            if (string.IsNullOrEmpty(key)) return true;   // NONE sentinel: always pass
            if (_conditions.TryGetValue(key, out var func))
            {
                return func.Invoke();
            }
            DevLog.Log(DevId.System, $"[TutorialRegistry] No condition registered for key: {key}");
            return false;
        }
    }
}
