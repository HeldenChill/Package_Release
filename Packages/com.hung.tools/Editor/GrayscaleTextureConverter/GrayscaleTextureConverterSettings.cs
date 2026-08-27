using UnityEditor;
using UnityEngine;

namespace GrayscaleTextureConverter
{
    public enum GrayscaleMethod
    {
        Luminance
    }

    public enum OutputFormat
    {
        PNG,
        JPG
    }

    public enum OutputLocation
    {
        SameAsSource,
        CustomFolder
    }

    public enum ExistingFileBehavior
    {
        Replace,
        UniqueFilename,
        Skip
    }

    [System.Serializable]
    public class GrayscaleTextureConverterSettings
    {
        // Conversion
        public GrayscaleMethod method = GrayscaleMethod.Luminance;
        public float brightness = 0f;
        public float contrast = 1f;
        public float gamma = 1f;
        public bool invert = false;
        public bool preserveAlpha = true;
        public bool colorize = false;
        public Color targetColor = Color.white;

        // Export
        public OutputFormat outputFormat = OutputFormat.PNG;
        public OutputLocation outputLocation = OutputLocation.SameAsSource;
        public string customOutputFolder = "Assets";
        public string filenameSuffix = "_Grayscale";
        public ExistingFileBehavior existingFileBehavior = ExistingFileBehavior.UniqueFilename;
        public int jpgQuality = 90;
        public bool overwriteOriginal = false;

        const string PrefPrefix = "GrayscaleTextureConverter.";

        public void Load()
        {
            brightness = EditorPrefs.GetFloat(PrefPrefix + "brightness", 0f);
            contrast = EditorPrefs.GetFloat(PrefPrefix + "contrast", 1f);
            gamma = EditorPrefs.GetFloat(PrefPrefix + "gamma", 1f);
            invert = EditorPrefs.GetBool(PrefPrefix + "invert", false);
            preserveAlpha = EditorPrefs.GetBool(PrefPrefix + "preserveAlpha", true);
            colorize = EditorPrefs.GetBool(PrefPrefix + "colorize", false);
            targetColor = new Color(
                EditorPrefs.GetFloat(PrefPrefix + "targetColorR", 1f),
                EditorPrefs.GetFloat(PrefPrefix + "targetColorG", 1f),
                EditorPrefs.GetFloat(PrefPrefix + "targetColorB", 1f),
                1f);

            outputFormat = (OutputFormat)EditorPrefs.GetInt(PrefPrefix + "outputFormat", (int)OutputFormat.PNG);
            outputLocation = (OutputLocation)EditorPrefs.GetInt(PrefPrefix + "outputLocation", (int)OutputLocation.SameAsSource);
            customOutputFolder = EditorPrefs.GetString(PrefPrefix + "customOutputFolder", "Assets");
            filenameSuffix = EditorPrefs.GetString(PrefPrefix + "filenameSuffix", "_Grayscale");
            existingFileBehavior = (ExistingFileBehavior)EditorPrefs.GetInt(PrefPrefix + "existingFileBehavior", (int)ExistingFileBehavior.UniqueFilename);
            jpgQuality = EditorPrefs.GetInt(PrefPrefix + "jpgQuality", 90);
            overwriteOriginal = EditorPrefs.GetBool(PrefPrefix + "overwriteOriginal", false);
        }

        public void Save()
        {
            EditorPrefs.SetFloat(PrefPrefix + "brightness", brightness);
            EditorPrefs.SetFloat(PrefPrefix + "contrast", contrast);
            EditorPrefs.SetFloat(PrefPrefix + "gamma", gamma);
            EditorPrefs.SetBool(PrefPrefix + "invert", invert);
            EditorPrefs.SetBool(PrefPrefix + "preserveAlpha", preserveAlpha);
            EditorPrefs.SetBool(PrefPrefix + "colorize", colorize);
            EditorPrefs.SetFloat(PrefPrefix + "targetColorR", targetColor.r);
            EditorPrefs.SetFloat(PrefPrefix + "targetColorG", targetColor.g);
            EditorPrefs.SetFloat(PrefPrefix + "targetColorB", targetColor.b);

            EditorPrefs.SetInt(PrefPrefix + "outputFormat", (int)outputFormat);
            EditorPrefs.SetInt(PrefPrefix + "outputLocation", (int)outputLocation);
            EditorPrefs.SetString(PrefPrefix + "customOutputFolder", customOutputFolder);
            EditorPrefs.SetString(PrefPrefix + "filenameSuffix", filenameSuffix);
            EditorPrefs.SetInt(PrefPrefix + "existingFileBehavior", (int)existingFileBehavior);
            EditorPrefs.SetInt(PrefPrefix + "jpgQuality", jpgQuality);
            EditorPrefs.SetBool(PrefPrefix + "overwriteOriginal", overwriteOriginal);
        }
    }
}
