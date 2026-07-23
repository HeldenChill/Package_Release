using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.Base
{
    public class MaskRaycastImage : Image
    {
        [SerializeField]
        List<RectTransform> maskRectTfs;
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (maskRectTfs != null)
            {
                for (int i = 0; i < maskRectTfs.Count; i++)
                {
                    if (maskRectTfs[i] == null) continue;
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRectTfs[i], screenPoint, eventCamera, out localPoint);
                    if (maskRectTfs[i].rect.Contains(localPoint))
                    {
                        return false;
                    }
                }

            }
            return base.IsRaycastLocationValid(screenPoint, eventCamera);
        }

    }
}
