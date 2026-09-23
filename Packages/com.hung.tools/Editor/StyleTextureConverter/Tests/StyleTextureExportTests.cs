using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace StyleTextureConverter.Tests
{
    public sealed class StyleTextureExportTests
    {
        const string Folder = "Assets/__StyleTextureExportTest";
        const string SourcePath = Folder + "/src.png";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets", "__StyleTextureExportTest");
            Texture2D tex = StyleTestUtil.MakeTexture(4, 4, new Color32[16]);
            File.WriteAllBytes(SourcePath, tex.EncodeToPNG());
            StyleTestUtil.Destroy(tex);
            AssetDatabase.ImportAsset(SourcePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SourcePath);
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_6x6
            });
            importer.SaveAndReimport();
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Folder);

        [Test]
        public void OutputPathAppendsSuffixAsPng()
        {
            Assert.AreEqual("Assets/A/b_Ice.png", StyleTextureExport.OutputPath("Assets/A/b.tga", "_Ice"));
        }

        [Test]
        public void EmptySuffixIsRefused()
        {
            Texture2D result = StyleTestUtil.MakeTexture(4, 4, new Color32[16]);
            try
            {
                Assert.IsNull(StyleTextureExport.Export(result, SourcePath, "", out string error));
                StringAssert.Contains("suffix", error);
            }
            finally
            {
                StyleTestUtil.Destroy(result);
            }
        }

        [Test]
        public void ExportMirrorsBaseMaxSizeAndNpotScale()
        {
            var sourceImporter = (TextureImporter)AssetImporter.GetAtPath(SourcePath);
            sourceImporter.maxTextureSize = 32;
            sourceImporter.npotScale = TextureImporterNPOTScale.None;
            sourceImporter.SaveAndReimport();

            Texture2D result = StyleTestUtil.MakeTexture(4, 4, new Color32[16]);
            try
            {
                string written = StyleTextureExport.Export(result, SourcePath, "_T", out string error);
                Assert.IsNull(error);
                var outImporter = (TextureImporter)AssetImporter.GetAtPath(written);
                Assert.AreEqual(32, outImporter.maxTextureSize);
                Assert.AreEqual(TextureImporterNPOTScale.None, outImporter.npotScale);
            }
            finally
            {
                StyleTestUtil.Destroy(result);
            }
        }

        [Test]
        public void TryDecodeSourceUsesFileSizeWhenImportIsCapped()
        {
            const string bigPath = Folder + "/big.png";
            Texture2D big = StyleTestUtil.MakeTexture(64, 64, new Color32[64 * 64]);
            File.WriteAllBytes(bigPath, big.EncodeToPNG());
            StyleTestUtil.Destroy(big);
            AssetDatabase.ImportAsset(bigPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(bigPath);
            importer.maxTextureSize = 32; // imported texture is now 32x32, file is 64x64
            importer.SaveAndReimport();
            Assert.AreEqual(32, AssetDatabase.LoadAssetAtPath<Texture2D>(bigPath).width);

            Assert.IsTrue(StyleTextureExport.TryDecodeSource(bigPath, out Texture2D decoded, out string error), error);
            try
            {
                Assert.AreEqual(64, decoded.width);
                Assert.AreEqual(64, decoded.height);
            }
            finally
            {
                StyleTestUtil.Destroy(decoded);
            }
        }

        [Test]
        public void ExportWritesBesideSourceLeavesSourceAndCopiesPlatformOverride()
        {
            byte[] sourceBytes = File.ReadAllBytes(SourcePath);
            Texture2D result = StyleTestUtil.MakeTexture(4, 4, new Color32[16]);
            try
            {
                string written = StyleTextureExport.Export(result, SourcePath, "_T", out string error);
                Assert.IsNull(error);
                Assert.AreEqual(Folder + "/src_T.png", written);
                CollectionAssert.AreEqual(sourceBytes, File.ReadAllBytes(SourcePath));

                var outImporter = (TextureImporter)AssetImporter.GetAtPath(written);
                TextureImporterPlatformSettings android = outImporter.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden);
                Assert.AreEqual(TextureImporterFormat.ASTC_6x6, android.format);
                Assert.AreEqual(1024, android.maxTextureSize);
            }
            finally
            {
                StyleTestUtil.Destroy(result);
            }
        }
    }
}
