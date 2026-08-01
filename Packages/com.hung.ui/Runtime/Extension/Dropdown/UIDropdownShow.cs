using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    public class UIDropdownShow : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField]
        UIDropdown dropDown;
        private void Awake()
        {
            dropDown.IsShowItem = true;
        }

        // Update is called once per frame
        private void OnDestroy()
        {
            dropDown.IsShowItem = false;
        }
    }
}