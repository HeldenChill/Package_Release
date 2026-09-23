using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StyleTextureConverter
{
    public sealed class StyleTextureConverterWindow : EditorWindow
    {
        const int PreviewMaxSide = 512;
        const double DebounceSeconds = 0.15;

        [SerializeField] Texture2D _source;
        [SerializeField] StyleProfile _profile;

        Texture2D _previewSource; // decoded + downscaled, owned
        Texture2D _previewResult; // owned; kept on step error so the last good image stays
        float _previewScale = 1f;
        string _error;
        double _rerunAt = -1;
        SerializedObject _profileSO;
        Texture2D _checkerTex;
        Vector2 _scroll;

        [MenuItem("Tools/Universal/Art/Image/Style Texture Converter")]
        static void Open() => GetWindow<StyleTextureConverterWindow>("Style Texture Converter");

        void OnEnable()
        {
            _checkerTex = BuildChecker();
            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += Schedule;
            RebuildPreviewSource();
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= Schedule;
            DestroyOwned(ref _previewSource);
            DestroyOwned(ref _previewResult);
            DestroyOwned(ref _checkerTex);
        }

        void Schedule() => _rerunAt = EditorApplication.timeSinceStartup + DebounceSeconds;

        void Tick()
        {
            if (_rerunAt < 0 || EditorApplication.timeSinceStartup < _rerunAt)
            {
                return;
            }
            _rerunAt = -1;
            RunPreview();
            Repaint();
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUI.BeginChangeCheck();
            _source = (Texture2D)EditorGUILayout.ObjectField("Source", _source, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildPreviewSource();
            }

            EditorGUI.BeginChangeCheck();
            _profile = (StyleProfile)EditorGUILayout.ObjectField("Profile", _profile, typeof(StyleProfile), false);
            if (EditorGUI.EndChangeCheck())
            {
                _profileSO = null;
                Schedule();
            }

            if (_profile != null)
            {
                DrawProfile();
            }

            if (_error != null)
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }

            DrawPreview();
            DrawExport();
            EditorGUILayout.EndScrollView();
        }

        void DrawProfile()
        {
            if (_profileSO == null || _profileSO.targetObject != _profile)
            {
                _profileSO = new SerializedObject(_profile);
            }

            _profileSO.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_profileSO.FindProperty(nameof(StyleProfile.outputSuffix)));
            EditorGUILayout.PropertyField(_profileSO.FindProperty(nameof(StyleProfile.steps)), true);
            if (EditorGUI.EndChangeCheck())
            {
                _profileSO.ApplyModifiedProperties();
                Schedule();
            }

            // The list's own '+' adds an empty managed-reference slot; the processor skips it.
            if (_profile.steps.Contains(null))
            {
                EditorGUILayout.HelpBox("Empty step slot (skipped). Use Add Step to pick a step type, then remove the empty slot.", MessageType.Warning);
            }

            if (GUILayout.Button("Add Step"))
            {
                ShowAddStepMenu();
            }
        }

        void ShowAddStepMenu()
        {
            var menu = new GenericMenu();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<StyleStep>()
                         // IsPublic hides the internal test-only steps (SetRedStep, ThrowStep); real steps must be public.
                         .Where(t => t.IsPublic && !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                         .OrderBy(t => t.Name))
            {
                Type stepType = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(stepType.Name)), false, () =>
                {
                    Undo.RecordObject(_profile, "Add Style Step");
                    _profile.steps.Add((StyleStep)Activator.CreateInstance(stepType));
                    EditorUtility.SetDirty(_profile);
                    _profileSO = null;
                    Schedule();
                });
            }
            menu.ShowAsContext();
        }

        void RebuildPreviewSource()
        {
            DestroyOwned(ref _previewSource);
            DestroyOwned(ref _previewResult);
            _error = null;
            if (_source == null)
            {
                return;
            }

            if (!TryDecodeSource(out Texture2D decoded))
            {
                return;
            }

            _previewSource = StyleTextureProcessor.Downscale(decoded, PreviewMaxSide, out _previewScale);
            DestroyImmediate(decoded);
            Schedule();
        }

        bool TryDecodeSource(out Texture2D decoded)
        {
            if (!StyleTextureExport.TryDecodeSource(AssetDatabase.GetAssetPath(_source), out decoded, out string error))
            {
                _error = error;
                return false;
            }
            return true;
        }

        void RunPreview()
        {
            if (_previewSource == null || _profile == null)
            {
                return;
            }

            try
            {
                Texture2D result = StyleTextureProcessor.Run(_previewSource, _profile, _previewScale);
                DestroyOwned(ref _previewResult);
                _previewResult = result;
                _error = null;
            }
            catch (StyleStepException e)
            {
                _error = e.Message;
            }
        }

        void DrawPreview()
        {
            if (_previewSource == null)
            {
                return;
            }

            float half = (position.width - 32f) / 2f;
            float height = half * _previewSource.height / _previewSource.width;
            Rect row = GUILayoutUtility.GetRect(position.width - 16f, height);
            var left = new Rect(row.x, row.y, half, height);
            var right = new Rect(row.x + half + 8f, row.y, half, height);

            DrawChecker(left);
            GUI.DrawTexture(left, _previewSource, ScaleMode.ScaleToFit, true);
            DrawChecker(right);
            if (_previewResult != null)
            {
                GUI.DrawTexture(right, _previewResult, ScaleMode.ScaleToFit, true);
            }
        }

        void DrawExport()
        {
            bool canExport = _source != null && _profile != null && _error == null
                             && !string.IsNullOrEmpty(_profile.outputSuffix);
            using (new EditorGUI.DisabledScope(!canExport))
            {
                if (GUILayout.Button("Export", GUILayout.Height(28f)))
                {
                    Export();
                }
            }
        }

        void Export()
        {
            string sourcePath = AssetDatabase.GetAssetPath(_source);
            string outPath = StyleTextureExport.OutputPath(sourcePath, _profile.outputSuffix);
            if (File.Exists(outPath) &&
                !EditorUtility.DisplayDialog("Style Texture Converter", $"'{outPath}' exists. Overwrite?", "Overwrite", "Cancel"))
            {
                return;
            }

            if (!TryDecodeSource(out Texture2D decoded))
            {
                return;
            }

            Texture2D result = null;
            try
            {
                result = StyleTextureProcessor.Run(decoded, _profile, 1f);
                string written = StyleTextureExport.Export(result, sourcePath, _profile.outputSuffix, out string error);
                if (written == null)
                {
                    _error = error;
                }
                else
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(written));
                }
            }
            catch (StyleStepException e)
            {
                _error = e.Message;
            }
            finally
            {
                DestroyImmediate(decoded);
                DestroyOwned(ref result);
            }
        }

        void DrawChecker(Rect rect) =>
            GUI.DrawTextureWithTexCoords(rect, _checkerTex, new Rect(0f, 0f, rect.width / 16f, rect.height / 16f));

        static Texture2D BuildChecker()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            Color a = new Color(0.8f, 0.8f, 0.8f);
            Color b = new Color(0.6f, 0.6f, 0.6f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, ((x / 8) + (y / 8)) % 2 == 0 ? a : b);
                }
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        static void DestroyOwned(ref Texture2D tex)
        {
            if (tex != null)
            {
                DestroyImmediate(tex);
                tex = null;
            }
        }
    }
}
