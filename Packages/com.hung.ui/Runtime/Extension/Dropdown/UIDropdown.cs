using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hung.UI
{
    public class UIDropdown : MonoBehaviour
    {
        public event Action<int> _OnClick;
        public event Action<int, int> _OnItemClick;
        public event Action<int ,int> _OnValueChange;

        bool isShowItem = false;
        [SerializeField]
        int indexID;
        //[SerializeField]
        public Dropdown dropdown;

        public bool IsShowItem
        {
            get => isShowItem;
            set
            {
                isShowItem = value;
                if (!isShowItem)
                {
                    _OnItemClick?.Invoke(indexID, dropdown.value);
                }
                    
            }
        }

        public int value => dropdown.value;
        private void Awake()
        {
            dropdown.onValueChanged.AddListener((value) => _OnValueChange?.Invoke(indexID, value));
        }

        public void AddOptions(List<string> options)
        {
            dropdown.AddOptions(options);
        }
        public void ClearOptions()
        {
            dropdown.ClearOptions();
        }
        public void OnPointerClick(BaseEventData eventData)
        {
            _OnClick?.Invoke(indexID);
        }
    }
}