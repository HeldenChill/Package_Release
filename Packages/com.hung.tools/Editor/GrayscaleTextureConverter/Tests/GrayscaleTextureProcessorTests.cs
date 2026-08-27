using NUnit.Framework;
using UnityEngine;

namespace GrayscaleTextureConverter.Tests
{
    public sealed class GrayscaleTextureProcessorTests
    {
        static Texture2D MakeTexture(params Color32[] pixels)
        {
            var texture = new Texture2D(pixels.Length, 1, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        static GrayscaleTextureConverterSettings Settings(bool colorize, Color target)
        {
            return new GrayscaleTextureConverterSettings
            {
                colorize = colorize,
                targetColor = target,
                brightness = 0f,
                contrast = 1f,
                gamma = 1f,
                invert = false,
                preserveAlpha = true
            };
        }

        static void Destroy(Texture2D texture)
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ColorizeDisabledKeepsGrayscaleRgb()
        {
            Texture2D source = MakeTexture(new Color32(255, 255, 255, 123));
            Texture2D result = null;
            try
            {
                result = GrayscaleTextureProcessor.Convert(
                    source, Settings(false, Color.white));
                Assert.AreEqual(new Color32(255, 255, 255, 123), result.GetPixels32()[0]);
            }
            finally
            {
                Destroy(result);
                Destroy(source);
            }
        }

        [Test]
        public void ColorizeWhiteGrayBecomesTargetColor()
        {
            Texture2D source = MakeTexture(new Color32(255, 255, 255, 200));
            Texture2D result = null;
            try
            {
                result = GrayscaleTextureProcessor.Convert(
                    source, Settings(true, new Color(1f, 0.25f, 0f, 1f)));
                Assert.AreEqual(new Color32(255, 64, 0, 200), result.GetPixels32()[0]);
            }
            finally
            {
                Destroy(result);
                Destroy(source);
            }
        }

        [Test]
        public void ColorizeBlackStaysBlack()
        {
            Texture2D source = MakeTexture(new Color32(0, 0, 0, 200));
            Texture2D result = null;
            try
            {
                result = GrayscaleTextureProcessor.Convert(
                    source, Settings(true, Color.red));
                Assert.AreEqual(new Color32(0, 0, 0, 200), result.GetPixels32()[0]);
            }
            finally
            {
                Destroy(result);
                Destroy(source);
            }
        }

        [Test]
        public void ColorizeMidGrayScalesTargetChannelsAndPreservesAlpha()
        {
            Texture2D source = MakeTexture(new Color32(128, 128, 128, 77));
            Texture2D result = null;
            try
            {
                result = GrayscaleTextureProcessor.Convert(
                    source, Settings(true, new Color(1f, 0.5f, 0f, 1f)));
                Assert.AreEqual(new Color32(128, 64, 0, 77), result.GetPixels32()[0]);
            }
            finally
            {
                Destroy(result);
                Destroy(source);
            }
        }

        [Test]
        public void DisablingAlphaWritesOpaqueOutput()
        {
            Texture2D source = MakeTexture(new Color32(255, 255, 255, 77));
            Texture2D result = null;
            try
            {
                var settings = Settings(true, Color.white);
                settings.preserveAlpha = false;
                result = GrayscaleTextureProcessor.Convert(source, settings);
                Assert.AreEqual(255, result.GetPixels32()[0].a);
            }
            finally
            {
                Destroy(result);
                Destroy(source);
            }
        }
    }
}
