using System;
using UnityEngine;

namespace Hung.Tool
{
    [Serializable]
    public class RectTransformSnapshot
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;
        public Vector2 offsetMin;
        public Vector2 offsetMax;

        public static RectTransformSnapshot From(RectTransform rect)
        {
            if (rect == null)
                return new RectTransformSnapshot();

            return new RectTransformSnapshot
            {
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                anchoredPosition = rect.anchoredPosition,
                sizeDelta = rect.sizeDelta,
                pivot = rect.pivot,
                localPosition = rect.localPosition,
                localEulerAngles = rect.localEulerAngles,
                localScale = rect.localScale,
                offsetMin = rect.offsetMin,
                offsetMax = rect.offsetMax
            };
        }

        public void ApplyTo(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.localScale = localScale;
            rect.localEulerAngles = localEulerAngles;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localPosition = localPosition;
        }
    }
}
