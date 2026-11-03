using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.Base.Init
{
    /// <summary>
    /// A standalone toggle control for the base init screen, independent of any UI library.
    /// </summary>
    public class InitToggle : MonoBehaviour
    {
        /// <summary>
        /// Raised when the toggle value changes. Parameters are (id, value).
        /// </summary>
        public Action<int, bool> ValueChanged;

        [SerializeField]
        int id;

        [SerializeField]
        Toggle toggle;

        /// <summary>
        /// Gets whether the toggle is currently on. False if toggle is null.
        /// </summary>
        public bool IsOn => toggle != null && toggle.isOn;

        /// <summary>
        /// Sets the toggle value. No-op if toggle is null.
        /// </summary>
        /// <param name="value">New toggle value.</param>
        public void SetValue(bool value)
        {
            if (toggle != null)
            {
                toggle.isOn = value;
            }
        }

        protected void Start()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnValueChange);
            }
        }

        private void OnValueChange(bool value)
        {
            ValueChanged?.Invoke(id, value);
        }

        protected void OnDestroy()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnValueChange);
            }
        }
    }
}
