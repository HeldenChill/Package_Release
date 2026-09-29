using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hung.Base;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Hung.Analytics.Editor
{
    /// <summary>
    /// Hung/Analytics/Settings: one row per backend (switch, categories, keys). Apply writes the
    /// HUNG_ANALYTICS_* scripting defines for Android, iOS and Standalone. Flags an enabled backend whose SDK
    /// is missing (would not compile) and a disabled backend whose SDK folder still ships native libraries.
    /// </summary>
    public sealed class AnalyticsSettingsWindow : EditorWindow
    {
        const string AssetPath = "Assets/Resources/" + AnalyticsBootstrap.SettingsResourcePath + ".asset";
        const string AppMetricaAutoRevenuePrefix = "APPMETRICA_FEATURES_ADREVENUE_";

        static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone };

        // Probe type (assembly-qualified) proves the SDK compiles here; folder is what must be deleted to strip natives.
        static readonly Dictionary<string, (string probeType, string folder)> Sdks = new Dictionary<string, (string, string)>
        {
            { AnalyticsBackendIds.Firebase, ("Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics", "Assets/Firebase") },
            { AnalyticsBackendIds.AppsFlyer, ("AppsFlyerSDK.AppsFlyer, AppsFlyer", "Assets/AppsFlyer") },
            { AnalyticsBackendIds.AppMetrica, ("Io.AppMetrica.AppMetrica, AppMetrica", "Assets/AppMetrica") },
            { AnalyticsBackendIds.GameAnalytics, ("GameAnalyticsSDK.GameAnalytics, GameAnalyticsSDK", "Assets/GameAnalytics") },
        };

        AnalyticsSettings _settings;
        SerializedObject _serialized;

        [MenuItem("Hung/Analytics/Settings")]
        static void Open() => GetWindow<AnalyticsSettingsWindow>("Analytics");

        /// <summary>Loads the settings asset, creating it with default rows if missing.</summary>
        public static AnalyticsSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AnalyticsSettings>(AssetPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                settings = CreateInstance<AnalyticsSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
            }
            foreach (var id in AnalyticsBackendIds.All)
                if (settings.Get(id) == null) settings.backends.Add(AnalyticsBackendEntry.CreateDefault(id));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>Writes one HUNG_ANALYTICS_* define per enabled backend on every target; other defines untouched.</summary>
        public static void ApplyDefines(AnalyticsSettings settings)
        {
            var enabledIds = settings.backends.Where(e => e != null && e.enabled).Select(e => e.id).ToArray();
            foreach (var target in Targets)
            {
                var current = PlayerSettings.GetScriptingDefineSymbols(target).Split(';');
                var next = AnalyticsBackendIds.ApplyDefines(current, enabledIds);
                if (!next.SequenceEqual(current.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim())))
                    PlayerSettings.SetScriptingDefineSymbols(target, next);
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        void OnEnable()
        {
            _settings = LoadOrCreate();
            _serialized = new SerializedObject(_settings);
        }

        void OnGUI()
        {
            _serialized.Update();
            EditorGUILayout.PropertyField(_serialized.FindProperty(nameof(AnalyticsSettings.debugLog)));
            var rows = _serialized.FindProperty(nameof(AnalyticsSettings.backends));
            bool blocked = false;

            for (int i = 0; i < rows.arraySize; i++)
            {
                var row = rows.GetArrayElementAtIndex(i);
                string id = row.FindPropertyRelative(nameof(AnalyticsBackendEntry.id)).stringValue;
                var enabled = row.FindPropertyRelative(nameof(AnalyticsBackendEntry.enabled));
                var categories = row.FindPropertyRelative(nameof(AnalyticsBackendEntry.categories));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"{id}  ({AnalyticsBackendIds.Define(id)})", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(enabled);
                EditorGUILayout.PropertyField(categories);
                if (id == AnalyticsBackendIds.AppsFlyer || id == AnalyticsBackendIds.AppMetrica)
                    EditorGUILayout.PropertyField(row.FindPropertyRelative(nameof(AnalyticsBackendEntry.apiKey)));
                if (id == AnalyticsBackendIds.AppsFlyer)
                    EditorGUILayout.PropertyField(row.FindPropertyRelative(nameof(AnalyticsBackendEntry.appId)), new GUIContent("iOS App Id"));

                foreach (var (message, type) in Problems(id, enabled.boolValue, (AnalyticsCategory)categories.intValue))
                {
                    EditorGUILayout.HelpBox(message, type);
                    blocked |= type == MessageType.Error;
                }
            }

            _serialized.ApplyModifiedProperties();
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(blocked))
            {
                if (GUILayout.Button("Apply scripting defines (recompiles)")) ApplyDefines(_settings);
            }
        }

        static IEnumerable<(string, MessageType)> Problems(string id, bool enabled, AnalyticsCategory categories)
        {
            if (!Sdks.TryGetValue(id, out var sdk)) yield break;
            bool present = Type.GetType(sdk.probeType) != null;
            if (enabled && !present)
                yield return ($"SDK not found ({sdk.probeType}). Import it or turn this backend off - Apply is blocked.", MessageType.Error);
            if (!enabled && AssetDatabase.IsValidFolder(sdk.folder))
                yield return ($"Off, but {sdk.folder} is still in the project: its native libraries still ship. Delete the folder to strip it.", MessageType.Warning);
            if (id == AnalyticsBackendIds.AppMetrica && enabled && (categories & AnalyticsCategory.Revenue) != 0
                && Targets.Any(t => PlayerSettings.GetScriptingDefineSymbols(t).Contains(AppMetricaAutoRevenuePrefix)))
                yield return ("AppMetrica auto ad-revenue adapters are on (APPMETRICA_FEATURES_ADREVENUE_*). Routing Revenue here too double-counts MAX/IronSource revenue.", MessageType.Warning);
        }
    }
}
