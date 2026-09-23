using System;
using UnityEngine;

namespace StyleTextureConverter
{
    public enum KeyChannel { Luminance, R, G, B, Max }

    /// <summary>
    /// Writes a 0..1 key per texel from one channel. Pick the channel that carries the art's shading
    /// (green for red-to-yellow fire): luminance flattens detail drawn with hue.
    /// </summary>
    [Serializable]
    public sealed class KeyExtractStep : StyleStep
    {
        public KeyChannel channel = KeyChannel.Luminance;

        [Tooltip("Remap the min/max measured over opaque texels to 0..1.")]
        public bool autoNormalize = true;

        [Range(0f, 1f)] public float alphaThreshold = 200f / 255f;

        public override string DisplayName => "Key Extract";

        public override void Apply(StyleBuffer buf)
        {
            Vector4[] px = buf.pixels;
            var key = new float[px.Length];
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < px.Length; i++)
            {
                float k = Read(px[i]);
                key[i] = k;
                if (px[i].w >= alphaThreshold)
                {
                    if (k < min) min = k;
                    if (k > max) max = k;
                }
            }

            // ponytail: no opaque texels or a flat key -> raw key kept
            if (autoNormalize && max > min)
            {
                float inv = 1f / (max - min);
                for (int i = 0; i < key.Length; i++)
                {
                    key[i] = Mathf.Clamp01((key[i] - min) * inv);
                }
            }

            buf.key = key;
        }

        float Read(Vector4 p)
        {
            switch (channel)
            {
                case KeyChannel.R: return p.x;
                case KeyChannel.G: return p.y;
                case KeyChannel.B: return p.z;
                case KeyChannel.Max: return Mathf.Max(p.x, Mathf.Max(p.y, p.z));
                default: return StyleBuffer.Luminance(p);
            }
        }
    }
}
