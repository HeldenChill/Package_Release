using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayscaleTextureConverter
{
    public class GrayscaleTextureConverterWindow : EditorWindow
    {
        [MenuItem("Tools/Universal/Art/Image/Grayscale Texture Converter")]
        static void Open()
        {
            var window = GetWindow<GrayscaleTextureConverterWindow>("Grayscale Converter");
            window.minSize = new Vector2(640, 560);
        }

        GrayscaleTextureConverterSettings _settings = new GrayscaleTextureConverterSettings();
        readonly List<Texture2D> _selected = new List<Texture2D>();
        Texture2D _newDropField;

        Vector2 _listScroll;
        Vector2 _settingsScroll;

        // Preview state
        Texture2D _previewSource;
        Texture2D _previewGrayscale; // temporary, never saved as asset
        Vector2 _previewOffset;
        float _previewZoom = 1f;
        Texture2D _checkerTex;

        void OnEnable()
        {
            _settings.Load();
            BuildCheckerTexture();
        }

        void OnDisable()
        {
            _settings.Save();
            DestroyPreviewGrayscale();
            if (_checkerTex != null)
            {
                DestroyImmediate(_checkerTex);
                _checkerTex = null;
            }
        }

        void BuildCheckerTexture()
        {
            const int size = 16;
            _checkerTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _checkerTex.hideFlags = HideFlags.HideAndDontSave;
            Color a = new Color(0.8f, 0.8f, 0.8f);
            Color b = new Color(0.6f, 0.6f, 0.6f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / 8) + (y / 8)) % 2 == 0;
                    _checkerTex.SetPixel(x, y, even ? a : b);
                }
            }
            _checkerTex.Apply();
            _checkerTex.wrapMode = TextureWrapMode.Repeat;
            _checkerTex.filterMode = FilterMode.Point;
        }

        void OnGUI()
        {
            HandleDragAndDrop();

            EditorGUILayout.Space(4);
            DrawSourceSection();
            EditorGUILayout.Space(6);
            DrawConversionSettings();
            EditorGUILayout.Space(6);
            DrawExportSettings();
            EditorGUILayout.Space(6);
            DrawPreviewSection();
            EditorGUILayout.Space(6);
            DrawActionButtons();
        }

        // ---------------------------------------------------------------
        // Source section
        // ---------------------------------------------------------------

        void DrawSourceSection()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Texture2D assets here", EditorStyles.helpBox);
            HandleDropArea(dropArea);

            EditorGUILayout.BeginHorizontal();
            _newDropField = (Texture2D)EditorGUILayout.ObjectField("Add Texture", _newDropField, typeof(Texture2D), false);
            if (_newDropField != null)
            {
                AddTexture(_newDropField);
                _newDropField = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Selected: {_selected.Count}");

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(100));
            for (int i = _selected.Count - 1; i >= 0; i--)
            {
                Texture2D tex = _selected[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(tex, typeof(Texture2D), false);
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    _selected.RemoveAt(i);
                    if (_previewSource == tex)
                    {
                        SetPreviewSource(null);
                    }
                }
                if (GUILayout.Button("Preview", GUILayout.Width(70)))
                {
                    SetPreviewSource(tex);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear List"))
            {
                _selected.Clear();
                SetPreviewSource(null);
            }
        }

        void HandleDragAndDrop()
        {
            // Global handling done via HandleDropArea on the specific rect.
        }

        void HandleDropArea(Rect dropArea)
        {
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
            {
                return;
            }

            if (evt.type == EventType.DragUpdated)
            {
                bool anyValid = DragAndDrop.objectReferences.Any(o => o is Texture2D);
                DragAndDrop.visualMode = anyValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is Texture2D tex)
                    {
                        AddTexture(tex);
                    }
                }
                evt.Use();
            }
        }

        void AddTexture(Texture2D tex)
        {
            if (tex == null || _selected.Contains(tex))
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[GrayscaleTextureConverter] Texture '{tex.name}' is not a project asset. Skipped.");
                return;
            }

            _selected.Add(tex);
            if (_previewSource == null)
            {
                SetPreviewSource(tex);
            }
        }

        // ---------------------------------------------------------------
        // Conversion settings
        // ---------------------------------------------------------------

        void DrawConversionSettings()
        {
            EditorGUILayout.LabelField("Conversion Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _settings.method = (GrayscaleMethod)EditorGUILayout.EnumPopup("Grayscale Method", _settings.method);
            _settings.brightness = EditorGUILayout.Slider("Brightness", _settings.brightness, -1f, 1f);
            _settings.contrast = EditorGUILayout.Slider("Contrast", _settings.contrast, 0f, 2f);
            _settings.gamma = EditorGUILayout.Slider("Gamma", _settings.gamma, 0.1f, 3f);
            _settings.invert = EditorGUILayout.Toggle("Invert", _settings.invert);
            _settings.preserveAlpha = EditorGUILayout.Toggle("Preserve Alpha", _settings.preserveAlpha);
            _settings.colorize = EditorGUILayout.Toggle("Colorize", _settings.colorize);
            using (new EditorGUI.DisabledScope(!_settings.colorize))
            {
                _settings.targetColor = EditorGUILayout.ColorField(
                    new GUIContent("Target Color"), _settings.targetColor, true, false, false);
            }

            if (EditorGUI.EndChangeCheck())
            {
                UpdatePreviewGrayscale();
            }
        }

        // ---------------------------------------------------------------
        // Export settings
        // ---------------------------------------------------------------

        void DrawExportSettings()
        {
            EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);

            _settings.outputFormat = (OutputFormat)EditorGUILayout.EnumPopup("Output Format", _settings.outputFormat);
            EditorGUILayout.HelpBox("PNG is used automatically for textures with transparency, regardless of this setting.", MessageType.None);

            _settings.outputLocation = (OutputLocation)EditorGUILayout.EnumPopup("Output Location", _settings.outputLocation);
            if (_settings.outputLocation == OutputLocation.CustomFolder)
            {
                EditorGUILayout.BeginHorizontal();
                _settings.customOutputFolder = EditorGUILayout.TextField("Custom Folder", _settings.customOutputFolder);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    string picked = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _settings.customOutputFolder = ToProjectRelativePath(picked);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            _settings.filenameSuffix = EditorGUILayout.TextField("Filename Suffix", _settings.filenameSuffix);
            _settings.existingFileBehavior = (ExistingFileBehavior)EditorGUILayout.EnumPopup("If File Exists", _settings.existingFileBehavior);

            if (_settings.outputFormat == OutputFormat.JPG)
            {
                _settings.jpgQuality = EditorGUILayout.IntSlider("JPG Quality", _settings.jpgQuality, 1, 100);
            }

            _settings.overwriteOriginal = EditorGUILayout.Toggle("Overwrite Original", _settings.overwriteOriginal);
            if (_settings.overwriteOriginal)
            {
                EditorGUILayout.HelpBox("Overwrite Original is enabled: the source texture file will be replaced.", MessageType.Warning);
            }
        }

        static string ToProjectRelativePath(string absolutePath)
        {
            absolutePath = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath;
            if (absolutePath.StartsWith(dataPath))
            {
                return "Assets" + absolutePath.Substring(dataPath.Length);
            }
            return absolutePath;
        }

        // ---------------------------------------------------------------
        // Preview
        // ---------------------------------------------------------------

        void SetPreviewSource(Texture2D tex)
        {
            _previewSource = tex;
            _previewOffset = Vector2.zero;
            _previewZoom = 1f;
            UpdatePreviewGrayscale();
        }

        void UpdatePreviewGrayscale()
        {
            DestroyPreviewGrayscale();

            if (_previewSource == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(_previewSource);
            Texture2D decoded = null;
            try
            {
                if (!GrayscaleTextureSourceDecoder.TryDecode(
                        path,
                        _previewSource.width,
                        _previewSource.height,
                        out decoded,
                        out string error))
                {
                    Debug.LogError($"[GrayscaleTextureConverter] Preview failed for '{path}': {error}");
                    return;
                }

                _previewGrayscale = GrayscaleTextureProcessor.Convert(decoded, _settings);
                _previewGrayscale.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GrayscaleTextureConverter] Preview failed for '{path}': {e.Message}");
            }
            finally
            {
                if (decoded != null)
                {
                    DestroyImmediate(decoded);
                }
            }
        }

        void DestroyPreviewGrayscale()
        {
            if (_previewGrayscale != null)
            {
                DestroyImmediate(_previewGrayscale);
                _previewGrayscale = null;
            }
        }

        void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _previewZoom = EditorGUILayout.Slider("Zoom", _previewZoom, 0.1f, 8f);
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                _previewZoom = 1f;
                _previewOffset = Vector2.zero;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Rect leftRect = GUILayoutUtility.GetRect(100, 260, GUILayout.ExpandWidth(true));
            Rect rightRect = GUILayoutUtility.GetRect(100, 260, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            DrawPreviewPanel(leftRect, _previewSource, "Original");
            DrawPreviewPanel(rightRect, _previewGrayscale, "Grayscale");

            HandlePan(leftRect);
            HandlePan(rightRect);
        }

        void DrawPreviewPanel(Rect rect, Texture2D tex, string label)
        {
            GUI.Box(rect, GUIContent.none);
            GUI.BeginGroup(rect);

            Rect innerRect = new Rect(0, 0, rect.width, rect.height);
            if (_checkerTex != null)
            {
                GUI.DrawTextureWithTexCoords(innerRect, _checkerTex, new Rect(0, 0, rect.width / 16f, rect.height / 16f));
            }

            if (tex != null)
            {
                float aspect = (float)tex.width / tex.height;
                float baseHeight = rect.height * 0.9f;
                float baseWidth = baseHeight * aspect;
                if (baseWidth > rect.width * 0.9f)
                {
                    baseWidth = rect.width * 0.9f;
                    baseHeight = baseWidth / aspect;
                }

                float w = baseWidth * _previewZoom;
                float h = baseHeight * _previewZoom;
                float cx = rect.width / 2f + _previewOffset.x;
                float cy = rect.height / 2f + _previewOffset.y;

                Rect texRect = new Rect(cx - w / 2f, cy - h / 2f, w, h);
                GUI.DrawTexture(texRect, tex, ScaleMode.StretchToFill, true);
            }

            GUI.EndGroup();
            GUI.Label(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 18), label, EditorStyles.whiteBoldLabel);

            if (tex != null)
            {
                GUI.Label(new Rect(rect.x + 4, rect.yMax - 18, rect.width - 8, 18), $"{tex.width} x {tex.height}");
            }
        }

        void HandlePan(Rect rect)
        {
            Event evt = Event.current;
            if (!rect.Contains(evt.mousePosition))
            {
                return;
            }

            if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                _previewOffset += evt.delta;
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.ScrollWheel)
            {
                _previewZoom = Mathf.Clamp(_previewZoom - evt.delta.y * 0.05f, 0.1f, 8f);
                evt.Use();
                Repaint();
            }
        }

        // ---------------------------------------------------------------
        // Actions
        // ---------------------------------------------------------------

        void DrawActionButtons()
        {
            bool hasValid = _selected.Count > 0;

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!hasValid))
            {
                if (GUILayout.Button("Convert Selected"))
                {
                    RunConversion(revealInProject: false);
                }
                if (GUILayout.Button("Convert and Reveal in Project"))
                {
                    RunConversion(revealInProject: true);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                _selected.Clear();
                SetPreviewSource(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        void RunConversion(bool revealInProject)
        {
            int total = _selected.Count;
            int successCount = 0;
            int failCount = 0;
            var producedPaths = new List<string>();
            var errors = new List<string>();

            try
            {
                for (int i = 0; i < total; i++)
                {
                    Texture2D tex = _selected[i];
                    string sourcePath = tex != null ? AssetDatabase.GetAssetPath(tex) : "<null>";

                    bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Grayscale Texture Converter",
                        $"Processing {i + 1}/{total}: {sourcePath}",
                        (float)i / total);

                    if (cancelled)
                    {
                        break;
                    }

                    string resultPath = ConvertOne(tex, out string error);
                    if (resultPath != null)
                    {
                        successCount++;
                        producedPaths.Add(resultPath);
                    }
                    else if (error != null)
                    {
                        failCount++;
                        errors.Add(error);
                        Debug.LogError($"[GrayscaleTextureConverter] {error}");
                    }
                    // resultPath == null && error == null => skipped by user setting, not a failure
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();

            if (producedPaths.Count == 1)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(producedPaths[0]);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
            else if (producedPaths.Count > 1)
            {
                string folder = Path.GetDirectoryName(producedPaths[0])?.Replace('\\', '/');
                var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
                if (folderAsset != null)
                {
                    EditorGUIUtility.PingObject(folderAsset);
                    if (revealInProject)
                    {
                        Selection.activeObject = folderAsset;
                    }
                }
            }

            string summary = $"Converted: {successCount}\nFailed: {failCount}\nTotal: {total}";
            if (errors.Count > 0)
            {
                summary += "\n\nErrors:\n" + string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                {
                    summary += $"\n...and {errors.Count - 10} more (see Console).";
                }
            }
            EditorUtility.DisplayDialog("Grayscale Conversion Complete", summary, "OK");
        }

        /// <summary>
        /// Converts a single texture end to end. Returns the output asset path on success,
        /// null with error==null if the file was skipped, or null with error set on failure.
        /// </summary>
        string ConvertOne(Texture2D tex, out string error)
        {
            error = null;

            if (tex == null)
            {
                error = "Texture reference is null.";
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(sourcePath))
            {
                error = $"'{tex.name}' is not a project asset (outside Assets/).";
                return null;
            }

            string absoluteSourcePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), sourcePath);
            if (!File.Exists(absoluteSourcePath))
            {
                error = $"Source file does not exist on disk: '{sourcePath}'.";
                return null;
            }

            if (tex.width <= 0 || tex.height <= 0)
            {
                error = $"'{sourcePath}' has zero width or height.";
                return null;
            }

            GrayscaleTextureImporterUtility.ImporterBackup backup =
                GrayscaleTextureImporterUtility.Capture(sourcePath);

            Texture2D grayscale = null;
            Texture2D decoded = null;
            string outputPath = null;

            try
            {
                if (!GrayscaleTextureSourceDecoder.TryDecode(
                        sourcePath,
                        tex.width,
                        tex.height,
                        out decoded,
                        out error))
                {
                    return null;
                }

                bool hasAlpha = TextureHasAlpha(decoded);

                try
                {
                    grayscale = GrayscaleTextureProcessor.Convert(decoded, _settings);
                }
                catch (System.Exception e)
                {
                    error = $"Conversion failed for '{sourcePath}': {e.Message}";
                    return null;
                }

                if (_settings.overwriteOriginal)
                {
                    outputPath = WriteOverwrite(tex, sourcePath, grayscale, hasAlpha, out error);
                }
                else
                {
                    outputPath = GrayscaleTextureExportUtility.Export(grayscale, hasAlpha, sourcePath, _settings, out error);
                    if (outputPath != null)
                    {
                        AssetDatabase.ImportAsset(outputPath);
                        GrayscaleTextureImporterUtility.ConfigureGeneratedTexture(outputPath, backup, hasAlpha);
                    }
                }

                return outputPath;
            }
            finally
            {
                if (grayscale != null)
                {
                    DestroyImmediate(grayscale);
                }
                if (decoded != null)
                {
                    DestroyImmediate(decoded);
                }
            }
        }

        string WriteOverwrite(Texture2D source, string sourcePath, Texture2D grayscale, bool hasAlpha, out string error)
        {
            error = null;

            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            bool isPng = extension == ".png";
            bool isJpg = extension == ".jpg" || extension == ".jpeg";

            if (!isPng && !isJpg)
            {
                error = $"Unsupported source format for overwrite: '{sourcePath}' ({extension}). Only PNG/JPG source files can be overwritten.";
                return null;
            }

            byte[] bytes = isPng
                ? grayscale.EncodeToPNG()
                : grayscale.EncodeToJPG(_settings.jpgQuality);

            if (bytes == null || bytes.Length == 0)
            {
                error = $"Encoding failed while overwriting '{sourcePath}'.";
                return null;
            }

            string absolutePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), sourcePath);

            try
            {
                File.WriteAllBytes(absolutePath, bytes);
            }
            catch (System.Exception e)
            {
                error = $"Failed to overwrite '{sourcePath}': {e.Message}";
                return null;
            }

            AssetDatabase.ImportAsset(sourcePath);
            return sourcePath;
        }

        static bool TextureHasAlpha(Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a != 255)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
