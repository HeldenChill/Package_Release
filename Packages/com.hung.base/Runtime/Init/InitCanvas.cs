using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Hung.Base.Init
{
    using Hung.UI;
    using Hung.Base;
    public class InitCanvas : UICanvas
    {
        public event Action<int, bool> _OnToggleValueChange;
        public event Action _OnStartGame;
        [SerializeField]
        UIToggle gridLogicDebugToggle;
        [SerializeField]
        UIToggle fpsDebugToggle;
        [SerializeField]
        UIToggle logToggle;
        [SerializeField]
        UIToggle showAdsToggle;
        [SerializeField]
        UIToggle iapToggle;
        [SerializeField]
        Button startButton;
        [SerializeField]
        TMP_InputField levelInputField;
        [SerializeField]
        public int StartLevel => int.Parse(levelInputField.text);
        void Start()
        {
            GameData.UserData data = Database.Load<GameData>(GameData.SaveKey).user;
            if (data != null)
                levelInputField.text = data.normalLevelIndex.ToString();
            gridLogicDebugToggle._OnValueChange = OnToggleValueChange;
            fpsDebugToggle._OnValueChange = OnToggleValueChange;
            logToggle._OnValueChange = OnToggleValueChange;
            showAdsToggle._OnValueChange += OnToggleValueChange;
            iapToggle._OnValueChange += OnToggleValueChange;

            startButton.onClick.AddListener(OnStartButtonClick);
        }

        public void SetData(bool value1, bool value2, bool value3, bool value4, bool value5)
        {
            gridLogicDebugToggle.SetValue(value1);
            fpsDebugToggle.SetValue(value2);
            logToggle.SetValue(value3);
            showAdsToggle.SetValue(value4);
            iapToggle.SetValue(value5);
        }

        private void OnToggleValueChange(int id, bool value)
        {
            _OnToggleValueChange?.Invoke(id, value);
        }

        private void OnStartButtonClick()
        {
            _OnStartGame?.Invoke();
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(OnStartButtonClick);
        }
    }
}
