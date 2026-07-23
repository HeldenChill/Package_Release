using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Utilities
{
    public static class UTILITIES
    {
        public static List<RaycastResult> results = new List<RaycastResult>();
        public static Vector3 GetPointOnRayAtY(Ray ray, float targetY)
        {
            float t = (targetY - ray.origin.y) / ray.direction.y;
            return ray.origin + ray.direction * t;
        }
        public static bool PercentRandom(float rate)
        {
            rate = Mathf.Clamp01(rate);
            float value = UnityEngine.Random.Range(0f, 1f);
            if (value < rate) return true;
            else return false;
        }
        public static bool PercentRandom(float rate, Action action)
        {
            if (PercentRandom(rate))
            {
                action?.Invoke();
                return true;
            }
            return false;
        }
        public static int WheelRandom(List<float> rates)
        {
            float totalRate = 0f;
            for (int i = 0; i < rates.Count; i++)
            {
                if (rates[i] < 0) rates[i] = 0;
                totalRate += rates[i];
            }

            float value = UnityEngine.Random.Range(0f, 1f) * totalRate;
            float currentAnchor = 0;
            for (int i = 0; i < rates.Count; i++)
            {
                if (currentAnchor <= value && value < rates[i] + currentAnchor)
                {
                    return i;
                }
                currentAnchor += rates[i];
            }
            return 0;
        }


        public static bool IsPointerOverUIObject(Vector2 clickPosition)
        {
            results.Clear();
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = clickPosition;
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }
        
        
    }
}
