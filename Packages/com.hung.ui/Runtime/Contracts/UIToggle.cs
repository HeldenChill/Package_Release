using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        [SerializeField]
        TMP_Text nameTxt;

        public bool IsOn => toggle.isOn;

        /// <summary>
        /// Sets the toggle label text. No-op if label text component is null.
        /// </summary>
        public void SetText(string text)
        {
            if (nameTxt != null) nameTxt.text = text;
        }

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
