using UnityEngine;

namespace StyleTextureConverter.Tests
{
    static class StyleTestUtil
    {
        public static Texture2D MakeTexture(int width, int height, params Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static StyleBuffer Buffer(int width, int height, params Color32[] pixels)
        {
            Texture2D texture = MakeTexture(width, height, pixels);
            try
            {
                return StyleTextureProcessor.ToBuffer(texture, 1f);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        public static void Destroy(Object obj)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    /// Test-only step: sets every pixel's red to a fixed value.
    [System.Serializable]
    sealed class SetRedStep : StyleStep
    {
        public float red;
        public override void Apply(StyleBuffer buf)
        {
            for (int i = 0; i < buf.pixels.Length; i++)
            {
                Vector4 p = buf.pixels[i];
                buf.pixels[i] = new Vector4(red, p.y, p.z, p.w);
            }
        }
    }

    [System.Serializable]
    sealed class ThrowStep : StyleStep
    {
        public override string DisplayName => "Thrower";
        public override void Apply(StyleBuffer buf) => throw new System.InvalidOperationException("boom");
    }
}
