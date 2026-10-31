using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.UI
{
    using Hung.UI;
    using Hung.Base;
    public class BasePopup : UISCanvas
    {
        [SerializeField]
        Button backgroundCloseButton;

        protected override void Start()
        {
            base.Start();
            backgroundCloseButton?.onClick.AddListener(Close);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            backgroundCloseButton?.onClick.RemoveListener(Close);
        }
        protected override void OnOpen(object param)
        {
            base.OnOpen(param);
            Locator.Audio?.PlaySfx(BaseAudioIds.UiPopupShow);
        }

        public override void Hide()
        {
            base.Hide();
            Locator.Audio?.PlaySfx(BaseAudioIds.UiPopupHide);
        }
        
        protected void LockBgCloseInteractable(bool isLock)
        {
            backgroundCloseButton.interactable = !isLock;
        }
    }
}
