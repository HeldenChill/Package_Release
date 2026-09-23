using System;
using UnityEngine;

namespace StyleTextureConverter
{
    /// <summary>Replaces RGB with ramp.Evaluate(key). No key yet -> luminance. Alpha untouched.</summary>
    [Serializable]
    public sealed class GradientRampStep : StyleStep
    {
        public Gradient ramp = new Gradient();

        public override string DisplayName => "Gradient Ramp";

        public override void Apply(StyleBuffer buf)
        {
            Vector4[] px = buf.pixels;
            float[] key = buf.key;
            for (int i = 0; i < px.Length; i++)
            {
                float k = key != null ? key[i] : StyleBuffer.Luminance(px[i]);
                Color c = ramp.Evaluate(k);
                px[i] = new Vector4(c.r, c.g, c.b, px[i].w);
            }
        }
    }
}
