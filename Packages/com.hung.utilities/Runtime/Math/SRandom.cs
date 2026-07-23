using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Utilities
{
    using System;
    public static class SRandom 
    {
        public static void Shuffle<T>(this T[] arr)
        {
            Random rng = new();
            int n = arr.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (arr[k], arr[n]) = (arr[n], arr[k]);
            }
        }

        public static void Shuffle(this List<int> list)
        {
            Random rng = new();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
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
        public static int WheelRandom(float[] rates)
        {
            float totalRate = 0f;
            for (int i = 0; i < rates.Length; i++)
            {
                if (rates[i] < 0) rates[i] = 0;
                totalRate += rates[i];
            }

            float value = UnityEngine.Random.Range(0f, 1f) * totalRate;
            float currentAnchor = 0;
            for (int i = 0; i < rates.Length; i++)
            {
                if (currentAnchor <= value && value < rates[i] + currentAnchor)
                {
                    return i;
                }
                currentAnchor += rates[i];
            }
            return 0;

        }
        public static (int, int) RectangleRandom(int minx, int maxx, int miny, int maxy)
        {
            int x, y;
            x = UnityEngine.Random.Range(minx, maxx);
            y = UnityEngine.Random.Range(miny, maxy);
            return (x, y);
        }
        public static List<int> IndexRandom(int startIndex, int endIndex, int count)
        {
            List<int> value = new List<int>();
            List<int> res = new List<int>();

            for (int i = startIndex; i < endIndex; i++)
            {
                value.Add(i);
            }

            count = value.Count < count ? value.Count : count;
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, value.Count);
                res.Add(value[index]);
                value.RemoveAt(index);
            }
            return res;
        }
        public static T PickRandom<T>(List<T> list)
        {
            if (list.Count == 0) return default(T);
            int index = UnityEngine.Random.Range(0, list.Count);
            return list[index];
        }
                public static void Shuffle<T>(this IList<T> ts, int seed)
        {
            var count = ts.Count;
            var last = count - 1;
            UnityEngine.Random.InitState(seed);
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
        public static void Shuffle<T>(this IList<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            UnityEngine.Random.InitState(Environment.TickCount);
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
        public static void ControlShuffle<T>(this IList<T> ts, int seed, int countSwap)
        {
            int count = ts.Count;
            if (count == 0) return;
            UnityEngine.Random.InitState(seed);
            for (var i = 0; i < countSwap; ++i)
            {
                int m = UnityEngine.Random.Range(0, count);
                int r = UnityEngine.Random.Range(0, count);
                var tmp = ts[m];
                ts[m] = ts[r];
                ts[r] = tmp;
            }
        }
        public static Vector3 DirectionRandom()
        {
            float x = UnityEngine.Random.Range(0, 1f);
            float y = UnityEngine.Random.Range(0, 1f);
            float z = UnityEngine.Random.Range(0, 1f);
            return new Vector3(x, y, z).normalized;
        }
    }
}