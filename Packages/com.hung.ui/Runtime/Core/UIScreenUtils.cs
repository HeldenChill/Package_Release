using UnityEngine;

namespace Hung.UI
{
    public static class UIScreenUtils
    {
        public static Vector2 WorldToUI(Canvas canvas, Vector3 worldPos, RectTransform target)
        {
            Vector3 screenPoint = canvas.worldCamera.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screenPoint, canvas.worldCamera, out Vector2 localPoint);
            return localPoint;
        }

        public static int PixelToUnitHeight(RectTransform parentCanvas, float pixel)
        {
            float unitHeight = parentCanvas.rect.height;
            float pixelHeight = Screen.height;
            return (int)(pixel / pixelHeight * unitHeight);
        }

        public static float BannerDp(RectTransform parentCanvas)
        {
            float unitHeight = parentCanvas.rect.height;
            float pixelHeight = Screen.height;
            return 168 / pixelHeight * unitHeight;
        }
    }
}
