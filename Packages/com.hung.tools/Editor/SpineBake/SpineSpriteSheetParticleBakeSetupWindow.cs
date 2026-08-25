#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SpineSpriteSheetParticleBakeSetupWindow : EditorWindow
{
    private const string LastSetupEditorPrefsKey =
        "SpineSpriteSheetParticleBakeSetupWindow.LastSetupGlobalObjectId";

    [SerializeField] private SpineSpriteSheetParticleBakeSetup setup;

    private SerializedObject setupSerializedObject;
    private Vector2 scroll;

    [MenuItem("Tools/PetVsMonster/Art/Particle/Spine Sprite Sheet Particle Setup")]
    public static void Open()
    {
        GetWindow<SpineSpriteSheetParticleBakeSetupWindow>("Spine Particle Setup");
    }

    private void OnEnable()
    {
        RestoreLastSetupReference();
        TryUseSelectedSetupIfMissing();
        RebuildSerializedObject();
    }

    private void OnDisable()
    {
        SaveLastSetupReference();
    }

    private void OnSelectionChange()
    {
        if (setup != null)
            return;

        TryUseSelectedSetupIfMissing();

        if (setup != null)
        {
            SaveLastSetupReference();
            RebuildSerializedObject();
            Repaint();
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSetupReferenceSection();
        DrawSetupEditorSection();
        DrawActionsSection();
        DrawInfoSection();
        DrawTexturePreviewSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSetupReferenceSection()
    {
        EditorGUILayout.LabelField("Scene Particle Bake Setup", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        setup = (SpineSpriteSheetParticleBakeSetup)EditorGUILayout.ObjectField(
            "Setup",
            setup,
            typeof(SpineSpriteSheetParticleBakeSetup),
            true
        );

        if (EditorGUI.EndChangeCheck())
        {
            SaveLastSetupReference();
            RebuildSerializedObject();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selected"))
                UseSelectedSetup();

            if (GUILayout.Button("Find In Scene"))
                FindSetupInScene();

            if (GUILayout.Button("Create In Scene"))
                CreateSetupInScene();

            GUI.enabled = setup != null;

            if (GUILayout.Button("Ping"))
                PingSetup();

            GUI.enabled = true;
        }

        EditorGUILayout.HelpBox(
            "This window edits SpineSpriteSheetParticleBakeSetup in the bake scene. The setup object holds the sprite sheet, grid, frame count and Particle System settings.",
            MessageType.Info
        );

        EditorGUILayout.Space(8f);
    }

    private void DrawSetupEditorSection()
    {
        if (setup == null)
        {
            EditorGUILayout.HelpBox(
                "No setup assigned. Click Create In Scene or Find In Scene.",
                MessageType.Warning
            );
            EditorGUILayout.Space(8f);
            return;
        }

        if (setupSerializedObject == null || setupSerializedObject.targetObject != setup)
            RebuildSerializedObject();

        setupSerializedObject.Update();

        EditorGUILayout.LabelField("Setup Fields", EditorStyles.boldLabel);

        SerializedProperty iterator = setupSerializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
                continue;
            }

            EditorGUILayout.PropertyField(iterator, true);
        }

        if (setupSerializedObject.ApplyModifiedProperties())
        {
            setup.Sanitize();
            EditorUtility.SetDirty(setup);
        }

        EditorGUILayout.Space(8f);
    }

    private void DrawActionsSection()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        GUI.enabled = setup != null;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Copy From Spine Bake Setup"))
            {
                setup.CopyBasicSettingsFromSpineBakeSetup();
                RebuildSerializedObject();
            }

            if (GUILayout.Button("Auto Detect Grid"))
            {
                setup.AutoDetectGridFromTextureSize();
                RebuildSerializedObject();
            }
        }

        if (GUILayout.Button("Apply Texture Import Settings"))
        {
            setup.ApplyRecommendedImportSettings();
            RebuildSerializedObject();
        }

        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = setup != null && setup.spriteSheet != null;

            if (GUILayout.Button("Create Particle In Scene", GUILayout.Height(34f)))
            {
                setup.CreateParticleInScene();
                RebuildSerializedObject();
            }

            if (GUILayout.Button("Create And Save Prefab", GUILayout.Height(34f)))
            {
                setup.CreateAndSavePrefab();
                RebuildSerializedObject();
            }

            GUI.enabled = setup != null;
        }

        GUI.enabled = true;

        EditorGUILayout.Space(8f);
    }

    private void DrawInfoSection()
    {
        if (setup == null)
            return;

        setup.Sanitize();

        int safeTilesX = Mathf.Max(1, setup.tilesX);
        int safeTilesY = Mathf.Max(1, setup.tilesY);
        int maxFrames = Mathf.Max(1, safeTilesX * safeTilesY);
        int safeFrameCount = Mathf.Clamp(setup.frameCount, 1, maxFrames);
        float lifetime = setup.CalculatedLifetime;

        EditorGUILayout.LabelField("Calculated Info", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            $"Grid: {safeTilesX} x {safeTilesY}\n" +
            $"Max Tile Count: {maxFrames}\n" +
            $"Frame Count Used: {safeFrameCount}\n" +
            $"Playback FPS: {Mathf.Max(1, setup.playbackFps)}\n" +
            $"Calculated Lifetime: {lifetime:0.###}s",
            MessageType.Info
        );

        if (setup.spriteSheet != null)
        {
            int expectedWidth = setup.sourceFrameWidth * safeTilesX;
            int expectedHeight = setup.sourceFrameHeight * safeTilesY;

            if (setup.spriteSheet.width != expectedWidth || setup.spriteSheet.height != expectedHeight)
            {
                EditorGUILayout.HelpBox(
                    $"Texture size is {setup.spriteSheet.width}x{setup.spriteSheet.height}, while frame size x grid is {expectedWidth}x{expectedHeight}. Check Source Frame Width/Height or click Auto Detect Grid.",
                    MessageType.Warning
                );
            }
        }

        EditorGUILayout.Space(8f);
    }

    private void DrawTexturePreviewSection()
    {
        if (setup == null || setup.spriteSheet == null)
            return;

        EditorGUILayout.LabelField("Sprite Sheet Preview", EditorStyles.boldLabel);

        Texture2D texture = setup.spriteSheet;
        float availableWidth = Mathf.Max(120f, position.width - 40f);
        float scale = Mathf.Min(1f, availableWidth / Mathf.Max(1, texture.width));
        float previewWidth = texture.width * scale;
        float previewHeight = texture.height * scale;

        Rect rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false));
        DrawCheckerboard(rect, 12f);
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);

        EditorGUILayout.Space(8f);
    }

    private void RebuildSerializedObject()
    {
        setupSerializedObject = setup != null ? new SerializedObject(setup) : null;
    }

    private void UseSelectedSetup()
    {
        GameObject selectedGameObject = Selection.activeGameObject;

        if (selectedGameObject == null)
        {
            Debug.LogWarning("[SpineSpriteSheetParticleBakeSetupWindow] No GameObject selected.");
            return;
        }

        SpineSpriteSheetParticleBakeSetup selectedSetup =
            selectedGameObject.GetComponent<SpineSpriteSheetParticleBakeSetup>();

        if (selectedSetup == null)
        {
            Debug.LogWarning("[SpineSpriteSheetParticleBakeSetupWindow] Selected GameObject does not have SpineSpriteSheetParticleBakeSetup.");
            return;
        }

        setup = selectedSetup;
        SaveLastSetupReference();
        RebuildSerializedObject();
        Repaint();
    }

    private void TryUseSelectedSetupIfMissing()
    {
        if (setup != null)
            return;

        GameObject selectedGameObject = Selection.activeGameObject;

        if (selectedGameObject == null)
            return;

        setup = selectedGameObject.GetComponent<SpineSpriteSheetParticleBakeSetup>();
    }

    private void FindSetupInScene()
    {
#if UNITY_2023_1_OR_NEWER
        setup = Object.FindFirstObjectByType<SpineSpriteSheetParticleBakeSetup>(
            FindObjectsInactive.Include
        );
#else
        setup = Object.FindObjectOfType<SpineSpriteSheetParticleBakeSetup>(true);
#endif

        if (setup == null)
        {
            Debug.LogWarning("[SpineSpriteSheetParticleBakeSetupWindow] No SpineSpriteSheetParticleBakeSetup found in the open scene.");
            return;
        }

        SaveLastSetupReference();
        RebuildSerializedObject();
        PingSetup();
        Repaint();
    }

    private void CreateSetupInScene()
    {
        GameObject go = new GameObject("Spine Sprite Sheet Particle Bake Setup");
        Undo.RegisterCreatedObjectUndo(go, "Create Spine Sprite Sheet Particle Bake Setup");

        setup = go.AddComponent<SpineSpriteSheetParticleBakeSetup>();
        setup.Sanitize();

        EditorUtility.SetDirty(setup);
        SaveLastSetupReference();
        RebuildSerializedObject();

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Repaint();
    }

    private void PingSetup()
    {
        if (setup == null)
            return;

        Selection.activeGameObject = setup.gameObject;
        EditorGUIUtility.PingObject(setup.gameObject);
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

        Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
        setup = obj as SpineSpriteSheetParticleBakeSetup;
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
}
#endif
