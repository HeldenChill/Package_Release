using System.Collections.Generic;
using Hung.Base;
using Hung.UI;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Hung.UI
{
    public class CallOutCanvas : UISCanvas
    {
        [SerializeField]
        protected List<RectTransform> calloutRectTfs;
        [SerializeField]
        protected List<Image> callOutImages;

        protected List<Sprite> defaultCallOutSprites;
        protected List<Image.Type> defaultCallOutTypes;
        protected void Awake()
        {
            defaultCallOutSprites = new List<Sprite>();
            defaultCallOutTypes = new List<Image.Type>();
            for (int i = 0; i < callOutImages.Count; i++)
            {
                defaultCallOutSprites.Add(callOutImages[i].sprite);
                defaultCallOutTypes.Add(callOutImages[i].type);
            }
        }
        public void Active(int calloutCount)
        {
            for (int i = 0; i < callOutImages.Count; i++)
            {
                callOutImages[i].gameObject.SetActive(i < calloutCount);
            }
        }
        protected override void OnClose()
        {
            base.OnClose();
            DevLog.Log(DevId.Hung, "Call Out Close!");
        }
        public void Target(Transform targetTf, Vector2 offset = default, Vector2 padding = default, Image image = null, int index = 0, bool isUI = true)
        {
            if (targetTf is RectTransform target)
            {
                // Match anchors
                calloutRectTfs[index].anchorMin = target.anchorMin;
                calloutRectTfs[index].anchorMax = target.anchorMax;

                // Match pivot
                calloutRectTfs[index].pivot = target.pivot;

                // Match position and size
                calloutRectTfs[index].position = target.position;

                calloutRectTfs[index].anchoredPosition += offset;
                if (isUI)
                {
                    calloutRectTfs[index].sizeDelta = target.sizeDelta + padding;    
                }
                else
                {
                    calloutRectTfs[index].sizeDelta = target.sizeDelta / Locator.UI.ParentCanvasTf.localScale.x + padding;
                }

                calloutRectTfs[index].rotation = target.rotation;
                calloutRectTfs[index].localScale = target.localScale;

                if (image != null)
                {
                    callOutImages[index].sprite = image.sprite;
                    callOutImages[index].type = image.type;
                }
                else
                {
                    callOutImages[index].sprite = defaultCallOutSprites[index];
                    callOutImages[index].type = defaultCallOutTypes[index];
                }
            }
        }
    }
}
