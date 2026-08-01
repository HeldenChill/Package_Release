using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.UI
{
    public class UIToggle : MonoBehaviour
    {
        public Action<int, bool> _OnValueChange;
        [SerializeField]
        int id;
        [SerializeField]
        Toggle toggle;
        public bool IsOn => toggle.isOn;

        protected void Start()
        {
            toggle.onValueChanged.AddListener(OnValueChange);
        }

        private void OnValueChange(bool value)
        {
            _OnValueChange?.Invoke(id, value);
        }

        public void SetValue(bool value)
        {
            toggle.isOn = value;
        }

        protected void OnDestroy()
        {
            toggle.onValueChanged.RemoveListener(OnValueChange);
        }
    }
}