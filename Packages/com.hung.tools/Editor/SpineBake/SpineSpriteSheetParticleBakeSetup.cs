using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class SpineSpriteSheetParticleBakeSetup : MonoBehaviour
{
    [Header("Sprite Sheet Source")]
    public Texture2D spriteSheet;
    public int tilesX = 8;
    public int tilesY = 8;
    public int frameCount = 64;

    [Header("Optional Sync From Spine Bake Setup")]
    [Tooltip("Used only by editor helper buttons. Put this component on the same GameObject as SpineSpriteSheetBakeSetup, or assign that component here.")]
    public MonoBehaviour spineSpriteSheetBakeSetup;
    public int sourceFrameWidth = 256;
    public int sourceFrameHeight = 256;

    [Header("Playback")]
    public int playbackFps = 24;
    public bool autoLifetimeFromFrameCount = true;
    public float startLifetime = 1f;
    public int cycleCount = 1;

    [Header("Particle")]
    public string objectName = "SpineSpriteSheet_Particle";
    public bool loop = false;
    public bool playOnAwake = true;
    public int burstCount = 1;
    public float startSize = 1f;
    public float startSpeed = 0f;
    public float gravityModifier = 0f;
    public int maxParticles = 32;
    public ParticleSystemSimulationSpace simulationSpace = ParticleSystemSimulationSpace.Local;
    public ParticleSystemScalingMode scalingMode = ParticleSystemScalingMode.Hierarchy;

    [Header("Renderer")]
    public Material particleMaterial;
    public bool assignTextureToMaterial = true;
    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
    public ParticleSystemRenderSpace alignment = ParticleSystemRenderSpace.View;
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;

    [Header("Import Settings")]
    public FilterMode filterMode = FilterMode.Bilinear;
    public bool disableMipMaps = true;
    public bool clampWrapMode = true;
    public bool uncompressedTexture = true;

    public float CalculatedLifetime
    {
        get
        {
            int safeFps = Mathf.Max(1, playbackFps);
            int safeFrameCount = Mathf.Max(1, frameCount);
            return autoLifetimeFromFrameCount ? safeFrameCount / (float)safeFps : Mathf.Max(0.01f, startLifetime);
        }
    }

    public void Sanitize()
    {
        tilesX = Mathf.Max(1, tilesX);
        tilesY = Mathf.Max(1, tilesY);
        frameCount = Mathf.Clamp(frameCount, 1, Mathf.Max(1, tilesX * tilesY));
        playbackFps = Mathf.Max(1, playbackFps);
        startLifetime = Mathf.Max(0.01f, startLifetime);
        cycleCount = Mathf.Max(1, cycleCount);
        burstCount = Mathf.Clamp(burstCount, 1, short.MaxValue);
        startSize = Mathf.Max(0.001f, startSize);
        maxParticles = Mathf.Max(1, maxParticles);
        sourceFrameWidth = Mathf.Max(1, sourceFrameWidth);
        sourceFrameHeight = Mathf.Max(1, sourceFrameHeight);

        if (string.IsNullOrWhiteSpace(objectName))
            objectName = spriteSheet != null ? spriteSheet.name + "_Particle" : "SpineSpriteSheet_Particle";
    }

#if UNITY_EDITOR
    [ContextMenu("Particle/Create In Scene")]
    public void CreateParticleInScene()
    {
        if (!ValidateForEditorCreate())
            return;

        GameObject particleObject = CreateConfiguredParticleObject(null);
        Undo.RegisterCreatedObjectUndo(particleObject, "Create Spine Sprite Sheet Particle");

        Selection.activeGameObject = particleObject;
        EditorGUIUtility.PingObject(particleObject);

        Debug.Log($"[SpineSpriteSheetParticleBakeSetup] Created particle object: {particleObject.name}");
    }

    [ContextMenu("Particle/Create And Save Prefab")]
    public void CreateAndSavePrefab()
    {
        if (!ValidateForEditorCreate())
            return;

        string safeName = GetSafeFileName(objectName);

        string prefabPath = EditorUtility.SaveFilePanelInProject(
            "Save Particle Prefab",
            safeName + ".prefab",
            "prefab",
            "Choose where to save the generated Particle System prefab."
        );

        if (string.IsNullOrEmpty(prefabPath))
            return;

        string folder = Path.GetDirectoryName(prefabPath);
        string materialAssetPath = null;

        if (particleMaterial == null || !EditorUtility.IsPersistent(particleMaterial))
        {
            materialAssetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(folder, safeName + "_Material.mat").Replace("\\", "/")
            );
        }

        GameObject particleObject = CreateConfiguredParticleObject(materialAssetPath);

        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAssetAndConnect(
            particleObject,
            prefabPath,
            InteractionMode.UserAction
        );

        if (prefabRoot != null)
        {
            Selection.activeObject = prefabRoot;
            EditorGUIUtility.PingObject(prefabRoot);
        }

        Debug.Log($"[SpineSpriteSheetParticleBakeSetup] Saved prefab: {prefabPath}");
    }

    [ContextMenu("Particle/Apply Recommended Texture Import Settings")]
    public void ApplyRecommendedImportSettings()
    {
        if (spriteSheet == null)
        {
            Debug.LogError("[SpineSpriteSheetParticleBakeSetup] Missing spriteSheet.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(spriteSheet);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[SpineSpriteSheetParticleBakeSetup] spriteSheet must be a project asset.");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError("[SpineSpriteSheetParticleBakeSetup] Could not read TextureImporter.");
            return;
        }

        Undo.RecordObject(importer, "Apply Sprite Sheet Particle Import Settings");

        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.filterMode = filterMode;

        if (disableMipMaps)
            importer.mipmapEnabled = false;

        if (clampWrapMode)
            importer.wrapMode = TextureWrapMode.Clamp;

        if (uncompressedTexture)
            importer.textureCompression = TextureImporterCompression.Uncompressed;

        importer.SaveAndReimport();

        Debug.Log($"[SpineSpriteSheetParticleBakeSetup] Applied texture import settings: {path}");
    }

    [ContextMenu("Particle/Auto Detect Grid From Texture Size")]
    public void AutoDetectGridFromTextureSize()
    {
        if (spriteSheet == null)
        {
            Debug.LogError("[SpineSpriteSheetParticleBakeSetup] Missing spriteSheet.");
            return;
        }

        Sanitize();

        if (spriteSheet.width % sourceFrameWidth != 0 || spriteSheet.height % sourceFrameHeight != 0)
        {
            Debug.LogWarning(
                $"[SpineSpriteSheetParticleBakeSetup] Texture size {spriteSheet.width}x{spriteSheet.height} is not divisible by frame size {sourceFrameWidth}x{sourceFrameHeight}. Grid may be wrong."
            );
        }

        Undo.RecordObject(this, "Auto Detect Particle Grid From Texture Size");

        tilesX = Mathf.Max(1, Mathf.RoundToInt(spriteSheet.width / (float)sourceFrameWidth));
        tilesY = Mathf.Max(1, Mathf.RoundToInt(spriteSheet.height / (float)sourceFrameHeight));
        frameCount = Mathf.Clamp(frameCount, 1, tilesX * tilesY);

        EditorUtility.SetDirty(this);

        Debug.Log($"[SpineSpriteSheetParticleBakeSetup] Auto detected grid: X={tilesX}, Y={tilesY}, Max Frames={tilesX * tilesY}");
    }

    [ContextMenu("Particle/Copy Basic Settings From Spine Bake Setup")]
    public void CopyBasicSettingsFromSpineBakeSetup()
    {
        MonoBehaviour spineSetup = ResolveSpineBakeSetup();

        if (spineSetup == null)
        {
            Debug.LogWarning("[SpineSpriteSheetParticleBakeSetup] Could not find SpineSpriteSheetBakeSetup on this GameObject or assigned field.");
            return;
        }

        Undo.RecordObject(this, "Copy Particle Settings From Spine Bake Setup");

        object columnsValue = GetFieldOrPropertyValue(spineSetup, "columns");
        object fpsValue = GetFieldOrPropertyValue(spineSetup, "fps");
        object fixedFrameCountValue = GetFieldOrPropertyValue(spineSetup, "fixedFrameCount");
        object frameWidthValue = GetFieldOrPropertyValue(spineSetup, "frameWidth");
        object frameHeightValue = GetFieldOrPropertyValue(spineSetup, "frameHeight");
        object animationNameValue = GetFieldOrPropertyValue(spineSetup, "animationName");

        if (columnsValue is int copiedColumns)
            tilesX = Mathf.Max(1, copiedColumns);

        if (fpsValue is int copiedFps)
            playbackFps = Mathf.Max(1, copiedFps);

        if (frameWidthValue is int copiedFrameWidth)
            sourceFrameWidth = Mathf.Max(1, copiedFrameWidth);

        if (frameHeightValue is int copiedFrameHeight)
            sourceFrameHeight = Mathf.Max(1, copiedFrameHeight);

        if (fixedFrameCountValue is int copiedFixedFrameCount && copiedFixedFrameCount > 0)
            frameCount = Mathf.Clamp(copiedFixedFrameCount, 1, Mathf.Max(1, tilesX * tilesY));

        if (animationNameValue is string copiedAnimationName && !string.IsNullOrWhiteSpace(copiedAnimationName))
            objectName = copiedAnimationName + "_Particle";

        if (spriteSheet != null)
            AutoDetectGridFromTextureSize();

        Sanitize();
        EditorUtility.SetDirty(this);

        Debug.Log("[SpineSpriteSheetParticleBakeSetup] Copied basic settings from Spine bake setup.");
    }

    private bool ValidateForEditorCreate()
    {
        Sanitize();

        if (spriteSheet == null)
        {
            Debug.LogError("[SpineSpriteSheetParticleBakeSetup] Missing spriteSheet.");
            return false;
        }

        return true;
    }

    private GameObject CreateConfiguredParticleObject(string materialAssetPath)
    {
        Sanitize();

        GameObject go = new GameObject(objectName);
        ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();

        ConfigureParticleSystem(particleSystem);
        ConfigureTextureSheetAnimation(particleSystem);
        ConfigureRenderer(particleRenderer, materialAssetPath);

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (playOnAwake)
            particleSystem.Play();

        return go;
    }

    private void ConfigureParticleSystem(ParticleSystem particleSystem)
    {
        float lifetime = CalculatedLifetime;

        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = loop;
        main.playOnAwake = playOnAwake;
        main.duration = Mathf.Max(0.01f, lifetime);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(gravityModifier);
        main.maxParticles = maxParticles;
        main.simulationSpace = simulationSpace;
        main.scalingMode = scalingMode;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = false;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = false;

        ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = false;

        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = false;

        ParticleSystem.NoiseModule noise = particleSystem.noise;
        noise.enabled = false;

        ParticleSystem.TrailModule trails = particleSystem.trails;
        trails.enabled = false;
    }

    private void ConfigureTextureSheetAnimation(ParticleSystem particleSystem)
    {
        ParticleSystem.TextureSheetAnimationModule textureSheet = particleSystem.textureSheetAnimation;

        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Grid;
        textureSheet.numTilesX = tilesX;
        textureSheet.numTilesY = tilesY;
        textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheet.useRandomRow = false;
        textureSheet.cycleCount = cycleCount;

        int totalTiles = Mathf.Max(1, tilesX * tilesY);
        int lastFrameIndex = Mathf.Clamp(frameCount - 1, 0, totalTiles - 1);

        float normalizedEndFrame = totalTiles <= 1
            ? 0f
            : lastFrameIndex / (float)(totalTiles - 1);

        AnimationCurve frameCurve = AnimationCurve.Linear(0f, 0f, 1f, normalizedEndFrame);

        textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, frameCurve);
    }

    private void ConfigureRenderer(ParticleSystemRenderer particleRenderer, string materialAssetPath)
    {
        particleRenderer.renderMode = renderMode;
        particleRenderer.alignment = alignment;
        particleRenderer.sortMode = ParticleSystemSortMode.None;
        particleRenderer.sortingLayerName = sortingLayerName;
        particleRenderer.sortingOrder = sortingOrder;

        particleRenderer.sharedMaterial = GetOrCreateMaterial(materialAssetPath);
    }

    private Material GetOrCreateMaterial(string materialAssetPath)
    {
        Material material = particleMaterial;

        if (material == null)
        {
            Shader shader = FindBestParticleShader();
            material = new Material(shader);
            material.name = GetSafeFileName(objectName) + "_Material";

            ApplyTextureToMaterial(material, spriteSheet);

            if (!string.IsNullOrEmpty(materialAssetPath))
            {
                AssetDatabase.CreateAsset(material, materialAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                particleMaterial = material;
                EditorUtility.SetDirty(this);
            }

            return material;
        }

        if (assignTextureToMaterial)
        {
            ApplyTextureToMaterial(material, spriteSheet);

            if (EditorUtility.IsPersistent(material))
            {
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
            }
        }

        return material;
    }

    private Shader FindBestParticleShader()
    {
        string[] shaderNames =
        {
            "Universal Render Pipeline/2D/Sprite-Unlit-Default",
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Sprites/Default",
            "Unlit/Transparent",
            "Unlit/Texture"
        };

        for (int i = 0; i < shaderNames.Length; i++)
        {
            Shader shader = Shader.Find(shaderNames[i]);
            if (shader != null)
                return shader;
        }

        return Shader.Find("Standard");
    }

    private void ApplyTextureToMaterial(Material material, Texture2D texture)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        TrySetTransparentBlendMode(material);
    }

    private void TrySetTransparentBlendMode(Material material)
    {
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 2f);

        if (material.HasProperty("_ColorMode"))
            material.SetFloat("_ColorMode", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private MonoBehaviour ResolveSpineBakeSetup()
    {
        if (spineSpriteSheetBakeSetup != null)
            return spineSpriteSheetBakeSetup;

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null || behaviour == this)
                continue;

            if (behaviour.GetType().Name == "SpineSpriteSheetBakeSetup")
            {
                spineSpriteSheetBakeSetup = behaviour;
                return behaviour;
            }
        }

        return null;
    }

    private object GetFieldOrPropertyValue(object target, string name)
    {
        if (target == null)
            return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = target.GetType();

        FieldInfo field = type.GetField(name, flags);
        if (field != null)
            return field.GetValue(target);

        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null)
            return property.GetValue(target, null);

        return null;
    }

    private string GetSafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "SpineSpriteSheet_Particle";

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar.ToString(), "_");

        return value.Trim();
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpineSpriteSheetParticleBakeSetup))]
public class SpineSpriteSheetParticleBakeSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpineSpriteSheetParticleBakeSetup setup = (SpineSpriteSheetParticleBakeSetup)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Particle Bake Actions", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Copy From Spine Bake Setup"))
                setup.CopyBasicSettingsFromSpineBakeSetup();

            if (GUILayout.Button("Auto Detect Grid"))
                setup.AutoDetectGridFromTextureSize();
        }

        if (GUILayout.Button("Apply Texture Import Settings"))
            setup.ApplyRecommendedImportSettings();

        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = setup.spriteSheet != null;

            if (GUILayout.Button("Create Particle In Scene", GUILayout.Height(32f)))
                setup.CreateParticleInScene();

            if (GUILayout.Button("Create And Save Prefab", GUILayout.Height(32f)))
                setup.CreateAndSavePrefab();

            GUI.enabled = true;
        }

        EditorGUILayout.Space(4f);

        float calculatedLifetime = setup.CalculatedLifetime;
        EditorGUILayout.HelpBox(
            $"Calculated Lifetime: {calculatedLifetime:0.###}s\n" +
            $"Grid: {Mathf.Max(1, setup.tilesX)} x {Mathf.Max(1, setup.tilesY)}\n" +
            $"Frames Used: {Mathf.Clamp(setup.frameCount, 1, Mathf.Max(1, setup.tilesX * setup.tilesY))}",
            MessageType.Info
        );
    }
}
#endif
