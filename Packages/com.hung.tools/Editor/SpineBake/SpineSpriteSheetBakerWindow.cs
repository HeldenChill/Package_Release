#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public class SpineSpriteSheetBakerWindow : EditorWindow
{
    private const string LastSetupEditorPrefsKey =
        "SpineSpriteSheetBakerWindow.LastSetupGlobalObjectId";
    private const string LastSpriteSheetSaveFolderKey =
        "SpineSpriteSheetBakerWindow.LastSpriteSheetSaveFolder";
    [Header("Scene Setup")]
    [SerializeField] private SpineSpriteSheetBakeSetup setup;
    [SerializeField] private bool autoSaveToSetup = true;

    [Header("Source")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private string animationName;

    [Header("Bake Settings")]
    [SerializeField] private SpineBakeSampleMode sampleMode = SpineBakeSampleMode.ByFps;
    [SerializeField] private int frameWidth = 256;
    [SerializeField] private int frameHeight = 256;
    [SerializeField] private int fps = 24;
    [SerializeField] private int fixedFrameCount = 64;
    [SerializeField] private bool includeLastFrame = false;
    [SerializeField] private int columns = 8;

    [Header("Empty Frame Removal")]
    [SerializeField] private bool removeEmptyFrames = true;
    [SerializeField] private int alphaThreshold = 0;

    [Header("Camera")]
    [SerializeField] private Camera bakeCamera;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);

    [Header("Preview")]
    [SerializeField] private bool loopPreview = true;
    [SerializeField] private int previewFps = 24;

    private readonly List<Texture2D> previewFrames = new List<Texture2D>();

    private Vector2 scroll;
    private int previewFrameIndex;
    private double lastPreviewTime;

    private int sourceFrameCount;
    private int keptFrameCount;
    private int removedFrameCount;
    private float animationDuration;

    [MenuItem("Tools/Universal/Art/Spine/Bake Sprite Sheet")]
    public static void Open()
    {
        GetWindow<SpineSpriteSheetBakerWindow>("Spine Sprite Sheet Baker");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;

        RestoreLastSetupReference();

        if (setup != null)
            LoadFromSetup();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;

        SaveLastSetupReference();
        ClearPreviewFrames();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSetupSection();

        EditorGUI.BeginChangeCheck();

        DrawSourceSettings();
        DrawBakeSettings();
        DrawEmptyFrameSettings();
        DrawCameraSettings();
        DrawPreviewSettings();

        bool changed = EditorGUI.EndChangeCheck();

        if (changed && autoSaveToSetup && setup != null)
            SaveToSetup();

        DrawActions();
        DrawBakeInfo();
        DrawPreview();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSetupSection()
    {
        EditorGUILayout.LabelField("Bake Scene Setup", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        setup = (SpineSpriteSheetBakeSetup)EditorGUILayout.ObjectField(
            "Setup",
            setup,
            typeof(SpineSpriteSheetBakeSetup),
            true
        );

        if (EditorGUI.EndChangeCheck())
        {
            SaveLastSetupReference();

            if (setup != null)
                LoadFromSetup();
        }

        autoSaveToSetup = EditorGUILayout.Toggle("Auto Save To Setup", autoSaveToSetup);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Find In Scene"))
                FindSetupInScene();

            if (GUILayout.Button("Create In Scene"))
                CreateSetupInScene();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = setup != null;

            if (GUILayout.Button("Load From Setup"))
                LoadFromSetup();

            if (GUILayout.Button("Save To Setup"))
                SaveToSetup();

            GUI.enabled = true;
        }

        EditorGUILayout.HelpBox(
            "Use a scene setup object to keep SkeletonAnimation, BakeCamera and bake settings after closing/reopening the editor window.",
            MessageType.Info
        );

        EditorGUILayout.Space();
    }

    private void DrawSourceSettings()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

        skeletonAnimation = (SkeletonAnimation)EditorGUILayout.ObjectField(
            "Skeleton Animation",
            skeletonAnimation,
            typeof(SkeletonAnimation),
            true
        );

        animationName = EditorGUILayout.TextField("Animation Name", animationName);

        if (skeletonAnimation != null && skeletonAnimation.SkeletonDataAsset != null)
        {
            DrawAnimationPopup();
        }

        EditorGUILayout.Space();
    }

    private void DrawAnimationPopup()
    {
        if (skeletonAnimation.SkeletonDataAsset == null)
            return;

        var skeletonData = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);

        if (skeletonData == null || skeletonData.Animations == null || skeletonData.Animations.Count == 0)
            return;

        string[] names = new string[skeletonData.Animations.Count];

        int currentIndex = 0;

        for (int i = 0; i < skeletonData.Animations.Count; i++)
        {
            names[i] = skeletonData.Animations.Items[i].Name;

            if (names[i] == animationName)
                currentIndex = i;
        }

        int selectedIndex = EditorGUILayout.Popup("Animation Popup", currentIndex, names);

        if (selectedIndex >= 0 && selectedIndex < names.Length)
            animationName = names[selectedIndex];
    }

    private void DrawBakeSettings()
    {
        EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);

        sampleMode = (SpineBakeSampleMode)EditorGUILayout.EnumPopup("Sample Mode", sampleMode);

        if (sampleMode == SpineBakeSampleMode.ByFps)
        {
            fps = EditorGUILayout.IntField("Bake FPS", fps);
            fps = Mathf.Max(1, fps);
        }
        else
        {
            fixedFrameCount = EditorGUILayout.IntField("Fixed Frame Count", fixedFrameCount);
            fixedFrameCount = Mathf.Max(1, fixedFrameCount);
        }

        includeLastFrame = EditorGUILayout.Toggle("Include Last Frame", includeLastFrame);

        frameWidth = EditorGUILayout.IntField("Frame Width", frameWidth);
        frameHeight = EditorGUILayout.IntField("Frame Height", frameHeight);
        columns = EditorGUILayout.IntField("Columns", columns);

        frameWidth = Mathf.Max(1, frameWidth);
        frameHeight = Mathf.Max(1, frameHeight);
        columns = Mathf.Max(1, columns);

        EditorGUILayout.Space();
    }

    private void DrawEmptyFrameSettings()
    {
        EditorGUILayout.LabelField("Empty Frame Removal", EditorStyles.boldLabel);

        removeEmptyFrames = EditorGUILayout.Toggle("Remove Empty Frames", removeEmptyFrames);
        alphaThreshold = EditorGUILayout.IntSlider("Alpha Threshold", alphaThreshold, 0, 255);

        EditorGUILayout.HelpBox(
            "A frame is considered empty when every pixel has alpha <= Alpha Threshold.",
            MessageType.None
        );

        EditorGUILayout.Space();
    }

    private void DrawCameraSettings()
    {
        EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);

        bakeCamera = (Camera)EditorGUILayout.ObjectField(
            "Bake Camera",
            bakeCamera,
            typeof(Camera),
            true
        );

        backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);

        EditorGUILayout.Space();
    }

    private void DrawPreviewSettings()
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        loopPreview = EditorGUILayout.Toggle("Loop Preview", loopPreview);
        previewFps = EditorGUILayout.IntField("Preview FPS", previewFps);
        previewFps = Mathf.Max(1, previewFps);

        EditorGUILayout.Space();
    }

    private void DrawActions()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Bake Preview", GUILayout.Height(32)))
                BakePreview();

            GUI.enabled = previewFrames.Count > 0;

            if (GUILayout.Button("Save Sprite Sheet PNG", GUILayout.Height(32)))
                SaveSpriteSheet();

            GUI.enabled = true;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Preview"))
            {
                ClearPreviewFrames();
                ResetBakeInfo();
            }

            if (GUILayout.Button("Select Setup") && setup != null)
            {
                Selection.activeObject = setup.gameObject;
                EditorGUIUtility.PingObject(setup.gameObject);
            }
        }

        EditorGUILayout.Space();
    }

    private void DrawBakeInfo()
    {
        if (sourceFrameCount <= 0)
            return;

        EditorGUILayout.LabelField("Bake Result", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Animation Duration", $"{animationDuration:0.000}s");
        EditorGUILayout.LabelField("Original Sample Frames", sourceFrameCount.ToString());
        EditorGUILayout.LabelField("Kept Frames", keptFrameCount.ToString());
        EditorGUILayout.LabelField("Removed Empty Frames", removedFrameCount.ToString());

        int rows = Mathf.CeilToInt(keptFrameCount / (float)columns);
        EditorGUILayout.LabelField("Sprite Sheet Grid", $"{columns} columns x {rows} rows");

        EditorGUILayout.Space();
    }

    private void DrawPreview()
    {
        if (previewFrames.Count == 0)
            return;

        EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);

        previewFrameIndex = Mathf.Clamp(previewFrameIndex, 0, previewFrames.Count - 1);
        Texture2D frame = previewFrames[previewFrameIndex];

        float availableWidth = Mathf.Max(100f, position.width - 40f);
        float scale = Mathf.Min(1f, availableWidth / frame.width);

        float previewWidth = frame.width * scale;
        float previewHeight = frame.height * scale;

        Rect rect = GUILayoutUtility.GetRect(
            previewWidth,
            previewHeight,
            GUILayout.ExpandWidth(false)
        );

        DrawCheckerboard(rect, 12f);
        GUI.DrawTexture(rect, frame, ScaleMode.ScaleToFit, true);

        EditorGUILayout.LabelField(
            "Current Frame",
            $"{previewFrameIndex + 1}/{previewFrames.Count}"
        );

        int selectedFrame = EditorGUILayout.IntSlider(
            "Scrub",
            previewFrameIndex,
            0,
            Mathf.Max(0, previewFrames.Count - 1)
        );

        if (selectedFrame != previewFrameIndex)
        {
            previewFrameIndex = selectedFrame;
            Repaint();
        }

        EditorGUILayout.Space();
    }

    private void OnEditorUpdate()
    {
        if (previewFrames.Count == 0)
            return;

        double now = EditorApplication.timeSinceStartup;
        double interval = 1.0 / Mathf.Max(1, previewFps);

        if (now - lastPreviewTime < interval)
            return;

        lastPreviewTime = now;

        if (previewFrameIndex < previewFrames.Count - 1)
        {
            previewFrameIndex++;
        }
        else if (loopPreview)
        {
            previewFrameIndex = 0;
        }

        Repaint();
    }

    private void BakePreview()
    {
        if (!ValidateBakeInput())
            return;

        if (autoSaveToSetup && setup != null)
            SaveToSetup();

        ClearPreviewFrames();
        ResetBakeInfo();

        try
        {
            BakeFramesToPreviewList();
            previewFrameIndex = 0;
            lastPreviewTime = EditorApplication.timeSinceStartup;

            Debug.Log(
                $"[SpineSpriteSheetBaker] Bake preview done. Original={sourceFrameCount}, Kept={keptFrameCount}, Removed={removedFrameCount}"
            );
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private bool ValidateBakeInput()
    {
        if (skeletonAnimation == null)
        {
            Debug.LogError("[SpineSpriteSheetBaker] Missing SkeletonAnimation.");
            return false;
        }

        if (bakeCamera == null)
        {
            Debug.LogError("[SpineSpriteSheetBaker] Missing Bake Camera.");
            return false;
        }

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogError("[SpineSpriteSheetBaker] Missing animation name.");
            return false;
        }

        frameWidth = Mathf.Max(1, frameWidth);
        frameHeight = Mathf.Max(1, frameHeight);
        fps = Mathf.Max(1, fps);
        fixedFrameCount = Mathf.Max(1, fixedFrameCount);
        columns = Mathf.Max(1, columns);
        previewFps = Mathf.Max(1, previewFps);

        skeletonAnimation.Initialize(false);

        if (skeletonAnimation.Skeleton == null || skeletonAnimation.Skeleton.Data == null)
        {
            Debug.LogError("[SpineSpriteSheetBaker] Skeleton is not initialized.");
            return false;
        }

        var animation = skeletonAnimation.Skeleton.Data.FindAnimation(animationName);

        if (animation == null)
        {
            Debug.LogError($"[SpineSpriteSheetBaker] Animation not found: {animationName}");
            return false;
        }

        return true;
    }

    private void BakeFramesToPreviewList()
    {
        var animation = skeletonAnimation.Skeleton.Data.FindAnimation(animationName);

        animationDuration = animation.Duration;
        sourceFrameCount = CalculateSourceFrameCount(animationDuration);

        if (sourceFrameCount <= 0)
        {
            Debug.LogError("[SpineSpriteSheetBaker] Animation has no frames to bake.");
            return;
        }

        RenderTexture renderTexture = new RenderTexture(
            frameWidth,
            frameHeight,
            24,
            RenderTextureFormat.ARGB32
        );

        Texture2D frameTexture = new Texture2D(
            frameWidth,
            frameHeight,
            TextureFormat.RGBA32,
            false
        );

        RenderTexture oldTargetTexture = bakeCamera.targetTexture;
        CameraClearFlags oldClearFlags = bakeCamera.clearFlags;
        Color oldBackgroundColor = bakeCamera.backgroundColor;
        RenderTexture oldActiveRenderTexture = RenderTexture.active;

        bakeCamera.targetTexture = renderTexture;
        bakeCamera.clearFlags = CameraClearFlags.SolidColor;
        bakeCamera.backgroundColor = backgroundColor;

        for (int i = 0; i < sourceFrameCount; i++)
        {
            float progress = sourceFrameCount <= 1
                ? 1f
                : i / (float)(sourceFrameCount - 1);

            EditorUtility.DisplayProgressBar(
                "Baking Spine Animation",
                $"Rendering frame {i + 1}/{sourceFrameCount}",
                progress
            );

            float sampleTime = GetSampleTime(i, sourceFrameCount, animationDuration);

            ApplySpinePose(sampleTime);

            bakeCamera.Render();

            RenderTexture.active = renderTexture;

            frameTexture.ReadPixels(
                new Rect(0, 0, frameWidth, frameHeight),
                0,
                0
            );

            frameTexture.Apply(false, false);

            Color32[] pixels = frameTexture.GetPixels32();
            bool hasVisiblePixel = HasVisiblePixel(pixels);

            if (removeEmptyFrames && !hasVisiblePixel)
            {
                removedFrameCount++;
                continue;
            }

            Texture2D copiedFrame = new Texture2D(
                frameWidth,
                frameHeight,
                TextureFormat.RGBA32,
                false
            );

            copiedFrame.SetPixels32(pixels);
            copiedFrame.Apply(false, false);

            previewFrames.Add(copiedFrame);
            keptFrameCount++;
        }

        bakeCamera.targetTexture = oldTargetTexture;
        bakeCamera.clearFlags = oldClearFlags;
        bakeCamera.backgroundColor = oldBackgroundColor;
        RenderTexture.active = oldActiveRenderTexture;

        renderTexture.Release();

        DestroyImmediate(renderTexture);
        DestroyImmediate(frameTexture);

        if (keptFrameCount <= 0)
            Debug.LogWarning("[SpineSpriteSheetBaker] All frames were removed as empty.");
    }

    private int CalculateSourceFrameCount(float duration)
    {
        if (sampleMode == SpineBakeSampleMode.FixedFrameCount)
            return Mathf.Max(1, fixedFrameCount);

        if (includeLastFrame)
            return Mathf.FloorToInt(duration * fps) + 1;

        return Mathf.Max(1, Mathf.CeilToInt(duration * fps));
    }

    private float GetSampleTime(int frameIndex, int totalFrames, float duration)
    {
        if (totalFrames <= 1)
            return 0f;

        if (sampleMode == SpineBakeSampleMode.FixedFrameCount)
        {
            if (includeLastFrame)
            {
                float normalized = frameIndex / (float)(totalFrames - 1);
                return Mathf.Clamp(duration * normalized, 0f, duration);
            }
            else
            {
                float normalized = frameIndex / (float)totalFrames;
                return Mathf.Clamp(duration * normalized, 0f, duration);
            }
        }

        float time = frameIndex / (float)fps;

        if (includeLastFrame && frameIndex == totalFrames - 1)
            time = duration;

        return Mathf.Clamp(time, 0f, duration);
    }

    private void ApplySpinePose(float time)
    {
        var state = skeletonAnimation.AnimationState;
        var skeleton = skeletonAnimation.Skeleton;

        state.ClearTracks();

        var trackEntry = state.SetAnimation(0, animationName, false);

        trackEntry.MixDuration = 0f;
        trackEntry.TrackTime = Mathf.Clamp(time, 0f, trackEntry.Animation.Duration);
        trackEntry.AnimationStart = 0f;
        trackEntry.AnimationEnd = trackEntry.Animation.Duration;

        state.Update(0f);
        state.Apply(skeleton);

        // Correct API for your current spine-unity version.
        skeleton.UpdateWorldTransform(Spine.Skeleton.Physics.Update);

        // Force the SkeletonRenderer mesh to refresh before camera render.
        skeletonAnimation.LateUpdate();
    }

    private bool HasVisiblePixel(Color32[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > alphaThreshold)
                return true;
        }

        return false;
    }

    private void SaveSpriteSheet()
    {
        if (previewFrames.Count <= 0)
        {
            Debug.LogError("[SpineSpriteSheetBaker] No preview frames available. Click Bake Preview first.");
            return;
        }

        Texture2D spriteSheet = BuildSpriteSheet(previewFrames);

        string path = EditorUtility.SaveFilePanel(
            "Save Spine Sprite Sheet",
            GetLastSpriteSheetSaveFolder(),
            $"{animationName}_spritesheet.png",
            "png"
        );

        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(spriteSheet);
            return;
        }

        SaveLastSpriteSheetSaveFolder(path);
        File.WriteAllBytes(path, spriteSheet.EncodeToPNG());
        DestroyImmediate(spriteSheet);

        AssetDatabase.Refresh();

        int rows = Mathf.CeilToInt(previewFrames.Count / (float)columns);

        Debug.Log($"[SpineSpriteSheetBaker] Saved sprite sheet: {path}");
        Debug.Log($"[SpineSpriteSheetBaker] Texture Sheet Animation Grid: X={columns}, Y={rows}");
        Debug.Log($"[SpineSpriteSheetBaker] Frames={previewFrames.Count}");
    }

    private Texture2D BuildSpriteSheet(List<Texture2D> frames)
    {
        int rows = Mathf.CeilToInt(frames.Count / (float)columns);

        int sheetWidth = columns * frameWidth;
        int sheetHeight = rows * frameHeight;

        Texture2D sheet = new Texture2D(
            sheetWidth,
            sheetHeight,
            TextureFormat.RGBA32,
            false
        );

        ClearTexture(sheet, new Color32(0, 0, 0, 0));

        for (int i = 0; i < frames.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            int dstX = col * frameWidth;

            // First frame goes to the top row.
            int dstY = sheetHeight - ((row + 1) * frameHeight);

            Color32[] pixels = frames[i].GetPixels32();

            sheet.SetPixels32(
                dstX,
                dstY,
                frameWidth,
                frameHeight,
                pixels
            );
        }

        sheet.Apply(false, false);
        return sheet;
    }

    private void ClearTexture(Texture2D texture, Color32 color)
    {
        Color32[] clearPixels = new Color32[texture.width * texture.height];

        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = color;

        texture.SetPixels32(clearPixels);
        texture.Apply(false, false);
    }

    private void ClearPreviewFrames()
    {
        for (int i = 0; i < previewFrames.Count; i++)
        {
            if (previewFrames[i] != null)
                DestroyImmediate(previewFrames[i]);
        }

        previewFrames.Clear();
        previewFrameIndex = 0;
    }

    private void ResetBakeInfo()
    {
        sourceFrameCount = 0;
        keptFrameCount = 0;
        removedFrameCount = 0;
        animationDuration = 0f;
    }

    private void DrawCheckerboard(Rect rect, float cellSize)
    {
        int cols = Mathf.CeilToInt(rect.width / cellSize);
        int rows = Mathf.CeilToInt(rect.height / cellSize);

        Color colorA = new Color(0.32f, 0.32f, 0.32f, 1f);
        Color colorB = new Color(0.22f, 0.22f, 0.22f, 1f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                bool even = (x + y) % 2 == 0;

                Rect cellRect = new Rect(
                    rect.x + x * cellSize,
                    rect.y + y * cellSize,
                    cellSize,
                    cellSize
                );

                EditorGUI.DrawRect(cellRect, even ? colorA : colorB);
            }
        }
    }

    private void FindSetupInScene()
    {
#if UNITY_2023_1_OR_NEWER
        setup = Object.FindFirstObjectByType<SpineSpriteSheetBakeSetup>(
            FindObjectsInactive.Include
        );
#else
        setup = Object.FindObjectOfType<SpineSpriteSheetBakeSetup>(true);
#endif

        if (setup == null)
        {
            Debug.LogWarning("[SpineSpriteSheetBaker] No SpineSpriteSheetBakeSetup found in the open scene.");
            return;
        }

        SaveLastSetupReference();
        LoadFromSetup();

        Selection.activeObject = setup.gameObject;
        EditorGUIUtility.PingObject(setup.gameObject);
    }

    private void CreateSetupInScene()
    {
        GameObject go = new GameObject("Spine Sprite Sheet Bake Setup");
        Undo.RegisterCreatedObjectUndo(go, "Create Spine Sprite Sheet Bake Setup");

        setup = go.AddComponent<SpineSpriteSheetBakeSetup>();

        SaveToSetup();
        SaveLastSetupReference();

        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void LoadFromSetup()
    {
        if (setup == null)
            return;

        skeletonAnimation = setup.skeletonAnimation;
        animationName = setup.animationName;

        sampleMode = setup.sampleMode;
        frameWidth = setup.frameWidth;
        frameHeight = setup.frameHeight;
        fps = setup.fps;
        fixedFrameCount = setup.fixedFrameCount;
        includeLastFrame = setup.includeLastFrame;
        columns = setup.columns;

        removeEmptyFrames = setup.removeEmptyFrames;
        alphaThreshold = setup.alphaThreshold;

        bakeCamera = setup.bakeCamera;
        backgroundColor = setup.backgroundColor;

        loopPreview = setup.loopPreview;
        previewFps = setup.previewFps;

        Repaint();
    }

    private void SaveToSetup()
    {
        if (setup == null)
            return;

        Undo.RecordObject(setup, "Save Spine Sprite Sheet Bake Setup");

        setup.skeletonAnimation = skeletonAnimation;
        setup.animationName = animationName;

        setup.sampleMode = sampleMode;
        setup.frameWidth = frameWidth;
        setup.frameHeight = frameHeight;
        setup.fps = fps;
        setup.fixedFrameCount = fixedFrameCount;
        setup.includeLastFrame = includeLastFrame;
        setup.columns = columns;

        setup.removeEmptyFrames = removeEmptyFrames;
        setup.alphaThreshold = alphaThreshold;

        setup.bakeCamera = bakeCamera;
        setup.backgroundColor = backgroundColor;

        setup.loopPreview = loopPreview;
        setup.previewFps = previewFps;

        EditorUtility.SetDirty(setup);

        SaveLastSetupReference();
    }

    private void SaveLastSetupReference()
    {
        if (setup == null)
        {
            EditorPrefs.DeleteKey(LastSetupEditorPrefsKey);
            return;
        }

        GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(setup);
        EditorPrefs.SetString(LastSetupEditorPrefsKey, id.ToString());
    }

    private void RestoreLastSetupReference()
    {
        if (!EditorPrefs.HasKey(LastSetupEditorPrefsKey))
            return;

        string idString = EditorPrefs.GetString(LastSetupEditorPrefsKey);

        if (string.IsNullOrEmpty(idString))
            return;

        if (!GlobalObjectId.TryParse(idString, out GlobalObjectId id))
            return;

        UnityEngine.Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);

        setup = obj as SpineSpriteSheetBakeSetup;
    }
    private string GetLastSpriteSheetSaveFolder()
    {
        string folder = EditorPrefs.GetString(
            LastSpriteSheetSaveFolderKey,
            Application.dataPath
        );

        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            folder = Application.dataPath;

        return folder;
    }

    private void SaveLastSpriteSheetSaveFolder(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        string folder = Path.GetDirectoryName(filePath);

        if (string.IsNullOrEmpty(folder))
            return;

        EditorPrefs.SetString(LastSpriteSheetSaveFolderKey, folder);
    }
}
#endif
