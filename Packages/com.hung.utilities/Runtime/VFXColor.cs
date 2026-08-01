using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Utilities
{
    public class VFXColor : MonoBehaviour
    {
        [SerializeField]
        ParticleSystem[] ps;

        public void SetStartColor(int index, Color color)
        {
            ParticleSystem.MainModule main = ps[index].main;
            main.startColor = color;
        }
        public void SetGradientColor(Color[] color, int[] indexColorKey)
        {
            for(int i = 0; i < color.Length; i++)
            {
                if (indexColorKey[i] < 0) continue;
                ParticleSystem.ColorOverLifetimeModule module = ps[i].colorOverLifetime;
                ParticleSystem.MinMaxGradient gradient = module.color;
                GradientColorKey[] colorKeys = gradient.gradient.colorKeys;
                colorKeys[indexColorKey[i]].color = color[i];
                gradient.gradient.SetKeys(colorKeys, gradient.gradient.alphaKeys);
                module.color = gradient;
            }
        }
    }
}
