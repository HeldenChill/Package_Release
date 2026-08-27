#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Hung.Tool
{
    public static class UIFlowPackageExporter
    {
        // Overridable per-project via EditorPrefs so this tool is not hardcoded to any one
        // project's folder layout. Falls back to this project's existing convention when unset,
        // so behavior here is unchanged; other projects can point these at their own UI folders.
        private const string ExportFolderPrefKey = "Hung.Tool.UIFlowPackageExporter.ExportFolder";
        private const string ScriptFolderPrefKey = "Hung.Tool.UIFlowPackageExporter.ScriptFolder";
        private const string EditorFolderPrefKey = "Hung.Tool.UIFlowPackageExporter.EditorFolder";

        private const string FallbackExportFolder = "Assets/_Game/_UI/Exports";
        private const string FallbackScriptFolder = "Assets/_Game/_UI/Scripts";
        private const string FallbackEditorFolder = "Assets/_Game/_UI/Editor/UIFlowGenerator";

        private static string DefaultExportFolder => EditorPrefs.GetString(ExportFolderPrefKey, FallbackExportFolder);
        private static string DefaultScriptFolder => EditorPrefs.GetString(ScriptFolderPrefKey, FallbackScriptFolder);
        private static string DefaultEditorFolder => EditorPrefs.GetString(EditorFolderPrefKey, FallbackEditorFolder);

        [MenuItem("Tools/Universal/UI Flow/Configure Export Folders")]
        public static void ConfigureExportFoldersMenu()
        {
            UIFlowExporterSettingsWindow.Open();
        }

        [MenuItem("Assets/Universal/UI Flow/Export Selected Definition Package")]
        public static void ExportSelectedDefinitionPackageMenu()
        {
            UIFlowDefinition definition = Selection.activeObject as UIFlowDefinition;
            if (definition == null)
            {
                Debug.LogError("[UIFlowPackageExporter] Please select a UIFlowDefinition asset.");
                return;
            }

            ExportDefinitionPackage(definition);
        }

        [MenuItem("Assets/Universal/UI Flow/Export Selected Pack Package")]
        public static void ExportSelectedPackPackageMenu()
        {
            UIFlowPack pack = Selection.activeObject as UIFlowPack;
            if (pack == null)
            {
                Debug.LogError("[UIFlowPackageExporter] Please select a UIFlowPack asset.");
                return;
            }

            ExportPackPackage(pack);
        }

        public static void ExportDefinitionPackage(UIFlowDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[UIFlowPackageExporter] Definition is null.");
                return;
            }

            EnsureFolderExists(DefaultExportFolder);

            string packageName = string.IsNullOrWhiteSpace(definition.canvasName)
                ? definition.name
                : definition.canvasName;

            string exportPath = EditorUtility.SaveFilePanel(
                "Export UI Flow Definition Package",
                ToAbsolutePath(DefaultExportFolder),
                $"{SanitizeFileName(packageName)}_UIFlow.unitypackage",
                "unitypackage");

            if (string.IsNullOrEmpty(exportPath))
                return;

            List<string> assetPaths = CollectDefinitionPackageAssetPaths(definition, includeGeneratorCore: true);
            ExportAssetPaths(assetPaths, exportPath);
        }

        public static void ExportPackPackage(UIFlowPack pack)
        {
            if (pack == null)
            {
                Debug.LogError("[UIFlowPackageExporter] Pack is null.");
                return;
            }

            EnsureFolderExists(DefaultExportFolder);

            string packageName = string.IsNullOrWhiteSpace(pack.packName)
                ? pack.name
                : pack.packName;

            string exportPath = EditorUtility.SaveFilePanel(
                "Export UI Flow Pack Package",
                ToAbsolutePath(DefaultExportFolder),
                $"{SanitizeFileName(packageName)}_UIFlowPack.unitypackage",
                "unitypackage");

            if (string.IsNullOrEmpty(exportPath))
                return;

            List<string> assetPaths = CollectPackPackageAssetPaths(pack, includeGeneratorCore: true);
            ExportAssetPaths(assetPaths, exportPath);
        }

        public static List<string> CollectDefinitionPackageAssetPaths(UIFlowDefinition definition, bool includeGeneratorCore)
        {
            var result = new HashSet<string>();

            AddAssetAndDependencies(result, definition);

            // Do not call definition.atlas directly.
            // Some UIFlowDefinition versions do not have an atlas field.
            CollectOptionalAssetField(result, definition, "atlas");
            CollectOptionalAssetField(result, definition, "spriteAtlas");
            CollectOptionalAssetListField(result, definition, "sharedAssets");

            CollectWidgetAssets(definition, result);
            CollectSnapshotAssets(definition, result);
            CollectGeneratedScripts(definition, result);

            if (includeGeneratorCore)
                CollectGeneratorCoreFiles(result);

            return NormalizeAssetPaths(result);
        }

        public static List<string> CollectPackPackageAssetPaths(UIFlowPack pack, bool includeGeneratorCore)
        {
            var result = new HashSet<string>();

            AddAssetAndDependencies(result, pack);

            if (pack.sharedAssets != null)
            {
                foreach (UnityEngine.Object asset in pack.sharedAssets)
                    AddAssetAndDependencies(result, asset);
            }

            if (pack.definitions != null)
            {
                foreach (UIFlowDefinition definition in pack.definitions)
                {
                    if (definition == null)
                        continue;

                    foreach (string path in CollectDefinitionPackageAssetPaths(definition, includeGeneratorCore: false))
                        result.Add(path);
                }
            }

            if (includeGeneratorCore)
                CollectGeneratorCoreFiles(result);

            return NormalizeAssetPaths(result);
        }

        public static void ExportAssetPaths(List<string> assetPaths, string exportPath)
        {
            if (assetPaths == null || assetPaths.Count == 0)
            {
                Debug.LogError("[UIFlowPackageExporter] No assets found to export.");
                return;
            }

            AssetDatabase.ExportPackage(
                assetPaths.ToArray(),
                exportPath,
                ExportPackageOptions.Interactive | ExportPackageOptions.Recurse);

            Debug.Log($"[UIFlowPackageExporter] Exported: {exportPath}\nAsset count: {assetPaths.Count}\n\n" + string.Join("\n", assetPaths));
        }

        private static void CollectWidgetAssets(UIFlowDefinition definition, HashSet<string> result)
        {
            if (definition == null || definition.widgets == null)
                return;

            foreach (WidgetSpec widget in definition.widgets)
            {
                if (widget == null)
                    continue;

                if (widget.sourcePrefab != null)
                    AddAssetAndDependencies(result, widget.sourcePrefab);
            }
        }

        private static void CollectSnapshotAssets(UIFlowDefinition definition, HashSet<string> result)
        {
            if (definition == null || definition.objectSnapshots == null)
                return;

            foreach (UIObjectSnapshot objectSnapshot in definition.objectSnapshots)
            {
                if (objectSnapshot == null)
                    continue;

                if (objectSnapshot.sourcePrefab != null)
                    AddAssetAndDependencies(result, objectSnapshot.sourcePrefab);

                if (!string.IsNullOrWhiteSpace(objectSnapshot.sourcePrefabPath))
                    AddAssetAndDependencies(result, objectSnapshot.sourcePrefabPath);

                if (objectSnapshot.components == null)
                    continue;

                foreach (UIComponentSnapshot componentSnapshot in objectSnapshot.components)
                {
                    if (componentSnapshot == null || componentSnapshot.assetReferences == null)
                        continue;

                    foreach (UIComponentAssetReference assetRef in componentSnapshot.assetReferences)
                    {
                        if (assetRef == null || assetRef.asset == null)
                            continue;

                        AddAssetAndDependencies(result, assetRef.asset);
                    }
                }
            }
        }

        private static void CollectGeneratedScripts(UIFlowDefinition definition, HashSet<string> result)
        {
            if (definition == null)
                return;

            string canvasName = string.IsNullOrWhiteSpace(definition.canvasName)
                ? definition.name
                : definition.canvasName;

            string generatedScriptPath = $"{DefaultScriptFolder}/{canvasName}.Generated.cs";
            string userScriptPath = $"{DefaultScriptFolder}/{canvasName}.cs";

            if (File.Exists(generatedScriptPath))
                result.Add(generatedScriptPath);

            if (File.Exists(userScriptPath))
                result.Add(userScriptPath);
        }

        private static void CollectGeneratorCoreFiles(HashSet<string> result)
        {
            AddIfExists(result, $"{DefaultEditorFolder}/BaseClass.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/WidgetSpec.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/FlowStep.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/ClickSpec.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFlowDefinition.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIObjectSnapShot.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIObjectSnapshot.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/RectTransformSnapshot.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIComponentSnapshot.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIComponentAssetReference.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFieldBindingSnapshot.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFlowPack.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFlowPackageExporter.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFlowCodeEmitter.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFlowPrefabBuilder.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFlowReverseImporter.cs");
            AddIfExists(result, $"{DefaultEditorFolder}/UIFlowGeneratorWindow.cs");
        }

        private static void CollectOptionalAssetField(HashSet<string> result, object owner, string fieldName)
        {
            if (result == null || owner == null || string.IsNullOrWhiteSpace(fieldName))
                return;

            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
                return;

            if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                return;

            UnityEngine.Object asset = field.GetValue(owner) as UnityEngine.Object;
            AddAssetAndDependencies(result, asset);
        }

        private static void CollectOptionalAssetListField(HashSet<string> result, object owner, string fieldName)
        {
            if (result == null || owner == null || string.IsNullOrWhiteSpace(fieldName))
                return;

            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
                return;

            object value = field.GetValue(owner);
            if (value is IEnumerable<UnityEngine.Object> assets)
            {
                foreach (UnityEngine.Object asset in assets)
                    AddAssetAndDependencies(result, asset);
            }
        }

        private static void AddIfExists(HashSet<string> result, string assetPath)
        {
            if (File.Exists(assetPath))
                result.Add(assetPath);
        }

        private static void AddAssetAndDependencies(HashSet<string> result, UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            string path = AssetDatabase.GetAssetPath(asset);
            AddAssetAndDependencies(result, path);
        }

        private static void AddAssetAndDependencies(HashSet<string> result, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            assetPath = assetPath.Replace("\\", "/");

            if (!assetPath.StartsWith("Assets/"))
                return;

            result.Add(assetPath);

            string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);
            foreach (string dependency in dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency))
                    continue;

                string normalized = dependency.Replace("\\", "/");
                if (!normalized.StartsWith("Assets/"))
                    continue;

                result.Add(normalized);
            }
        }

        private static List<string> NormalizeAssetPaths(HashSet<string> paths)
        {
            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace("\\", "/"))
                .Where(path => path.StartsWith("Assets/"))
                .Distinct()
                .OrderBy(path => path)
                .ToList();
        }

        private static string ToAbsolutePath(string assetFolderPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName.Replace("\\", "/");
            if (string.IsNullOrEmpty(projectRoot))
                return Application.dataPath;

            return Path.Combine(projectRoot, assetFolderPath).Replace("\\", "/");
        }

        private static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "UIFlow";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                raw = raw.Replace(invalid, '_');

            return raw.Trim();
        }

        private static void EnsureFolderExists(string assetFolderPath)
        {
            assetFolderPath = assetFolderPath.Replace("\\", "/").Trim('/');

            if (AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }

    public class UIFlowExporterSettingsWindow : EditorWindow
    {
        public static void Open()
        {
            GetWindow<UIFlowExporterSettingsWindow>("UI Flow Exporter Settings");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "These folders control where UIFlowPackageExporter looks for/exports generator files. " +
                "Defaults match this project's convention; change them if this package is used in a different project.",
                MessageType.Info);

            EditorGUILayout.Space(8);

            DrawFolderField("Export Folder", "Hung.Tool.UIFlowPackageExporter.ExportFolder", "Assets/_Game/_UI/Exports");
            DrawFolderField("Script Folder", "Hung.Tool.UIFlowPackageExporter.ScriptFolder", "Assets/_Game/_UI/Scripts");
            DrawFolderField("Editor Folder", "Hung.Tool.UIFlowPackageExporter.EditorFolder", "Assets/_Game/_UI/Editor/UIFlowGenerator");

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Reset To Defaults"))
            {
                EditorPrefs.DeleteKey("Hung.Tool.UIFlowPackageExporter.ExportFolder");
                EditorPrefs.DeleteKey("Hung.Tool.UIFlowPackageExporter.ScriptFolder");
                EditorPrefs.DeleteKey("Hung.Tool.UIFlowPackageExporter.EditorFolder");
            }
        }

        private static void DrawFolderField(string label, string prefKey, string fallback)
        {
            string current = EditorPrefs.GetString(prefKey, fallback);
            string updated = EditorGUILayout.TextField(label, current);

            if (updated != current)
                EditorPrefs.SetString(prefKey, updated);
        }
    }
}
#endif
