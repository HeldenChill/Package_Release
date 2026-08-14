using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.UI
{
    using Hung.UI;
    public class Type4ButtonComponent : UIButtonComponent
    {
        [SerializeField]
        Image IndicatorImage;
        [SerializeField]
        Image LockingImage;
        [SerializeField]
        Image BlockRaycastImage;
        public override void SetState(UIButton.STATE state){}
        public void SetIndicator(bool isIndicator)
        {
            IndicatorImage.gameObject.SetActive(isIndicator);
        }
        public void SetLocking(bool isLocking)
        {
            LockingImage.gameObject.SetActive(isLocking);
        }
        public void SetBlockRaycast(bool isBlockRaycast) 
        { 
            BlockRaycastImage.gameObject.SetActive(isBlockRaycast);
        }
    }
}