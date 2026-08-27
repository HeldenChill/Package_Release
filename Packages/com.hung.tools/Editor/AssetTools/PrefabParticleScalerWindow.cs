#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PrefabParticleScalerWindow : EditorWindow
{
    private GameObject targetPrefabOrRoot;
    private float scaleFactor = 1f;
    private bool includeInactive = true;
    private bool autoPickSelection = true;
    private ScaleMode scaleMode = ScaleMode.FullSpatialEffect;

    private enum ScaleMode
    {
        VisualSizeOnly,
        FullSpatialEffect
    }

    [MenuItem("Tools/Universal/Art/Particle/Prefab Particle Scaler")]
    public static void Open()
    {
        GetWindow<PrefabParticleScalerWindow>("Prefab Particle Scaler");
    }

    private void OnEnable()
    {
        TryPickSelection();
    }

    private void OnSelectionChange()
    {
        if (!autoPickSelection) return;

        TryPickSelection();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Prefab Particle Scaler", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Scale trực tiếp thông số ParticleSystem, không chỉnh Transform localScale.\n" +
            "Mỗi lần Apply sẽ nhân tiếp theo Scale Factor hiện tại.",
            MessageType.Info
        );

        autoPickSelection = EditorGUILayout.Toggle("Auto Pick Selection", autoPickSelection);

        targetPrefabOrRoot = (GameObject)EditorGUILayout.ObjectField(
            "Prefab / Root",
            targetPrefabOrRoot,
            typeof(GameObject),
            true
        );

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        scaleFactor = EditorGUILayout.FloatField("Scale Factor", scaleFactor);
        scaleMode = (ScaleMode)EditorGUILayout.EnumPopup("Scale Mode", scaleMode);

        DrawPresetButtons();

        EditorGUILayout.Space(8);

        int count = CountParticleSystems(targetPrefabOrRoot);
        EditorGUILayout.LabelField("Particle Systems Found", count.ToString());

        using (new EditorGUI.DisabledScope(targetPrefabOrRoot == null || scaleFactor <= 0f || count <= 0))
        {
            if (GUILayout.Button("Apply Particle Scale", GUILayout.Height(36)))
            {
                ApplyScale();
            }
        }

        if (scaleFactor <= 0f)
        {
            EditorGUILayout.HelpBox("Scale Factor phải lớn hơn 0.", MessageType.Warning);
        }
    }

    private void DrawPresetButtons()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("x0.25")) scaleFactor = 0.25f;
        if (GUILayout.Button("x0.5")) scaleFactor = 0.5f;
        if (GUILayout.Button("x0.75")) scaleFactor = 0.75f;
        if (GUILayout.Button("x1.25")) scaleFactor = 1.25f;
        if (GUILayout.Button("x1.5")) scaleFactor = 1.5f;
        if (GUILayout.Button("x2")) scaleFactor = 2f;

        EditorGUILayout.EndHorizontal();
    }

    private void TryPickSelection()
    {
        if (Selection.activeGameObject != null)
        {
            targetPrefabOrRoot = Selection.activeGameObject;
        }
    }

    private int CountParticleSystems(GameObject root)
    {
        if (root == null) return 0;
        return root.GetComponentsInChildren<ParticleSystem>(includeInactive).Length;
    }

    private void ApplyScale()
    {
        if (targetPrefabOrRoot == null) return;
        if (scaleFactor <= 0f) return;

        if (PrefabUtility.IsPartOfPrefabAsset(targetPrefabOrRoot))
        {
            ApplyToPrefabAsset(targetPrefabOrRoot);
        }
        else
        {
            ApplyToSceneRoot(targetPrefabOrRoot);
        }
    }

    private void ApplyToPrefabAsset(GameObject prefabAsset)
    {
        string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("PrefabParticleScaler: Không tìm thấy đường dẫn prefab asset.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            ParticleSystem[] particleSystems = prefabRoot.GetComponentsInChildren<ParticleSystem>(includeInactive);

            foreach (ParticleSystem ps in particleSystems)
            {
                ScaleParticleSystem(ps, scaleFactor);
                EditorUtility.SetDirty(ps);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"PrefabParticleScaler: Scaled {particleSystems.Length} ParticleSystem(s) in prefab asset: {prefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private void ApplyToSceneRoot(GameObject root)
    {
        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(includeInactive);

        var undoObjects = new List<Object>();
        undoObjects.AddRange(particleSystems.Select(x => (Object)x));

        Undo.RecordObjects(undoObjects.ToArray(), "Scale Particle Systems");

        foreach (ParticleSystem ps in particleSystems)
        {
            ScaleParticleSystem(ps, scaleFactor);
            EditorUtility.SetDirty(ps);

            if (PrefabUtility.IsPartOfPrefabInstance(ps))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(ps);
            }
        }

        Debug.Log($"PrefabParticleScaler: Scaled {particleSystems.Length} ParticleSystem(s) in scene object: {root.name}");
    }

    private void ScaleParticleSystem(ParticleSystem ps, float factor)
    {
        ScaleMainModule(ps, factor);
        ScaleEmissionModule(ps, factor);
        ScaleShapeModule(ps, factor);
        ScaleTextureSheetAnimationModule(ps, factor);

        if (scaleMode == ScaleMode.FullSpatialEffect)
        {
            ScaleVelocityOverLifetimeModule(ps, factor);
            ScaleLimitVelocityOverLifetimeModule(ps, factor);
            ScaleForceOverLifetimeModule(ps, factor);
            ScaleNoiseModule(ps, factor);
            ScaleExternalForcesModule(ps, factor);
        }
    }

    private void ScaleMainModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.MainModule main = ps.main;

        if (main.startSize3D)
        {
            main.startSizeX = ScaleMinMaxCurve(main.startSizeX, factor);
            main.startSizeY = ScaleMinMaxCurve(main.startSizeY, factor);
            main.startSizeZ = ScaleMinMaxCurve(main.startSizeZ, factor);
        }
        else
        {
            main.startSize = ScaleMinMaxCurve(main.startSize, factor);
        }

        if (scaleMode == ScaleMode.FullSpatialEffect)
        {
            main.startSpeed = ScaleMinMaxCurve(main.startSpeed, factor);
            main.gravityModifier = ScaleMinMaxCurve(main.gravityModifier, factor);
        }
    }

    private void ScaleEmissionModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.EmissionModule emission = ps.emission;
        if (!emission.enabled) return;

        // Không scale rateOverTime/rateOverDistance mặc định để tránh effect bị dày bất thường.
        // Burst count cũng giữ nguyên để scale không làm thay đổi mật độ particle quá mạnh.
    }

    private void ScaleShapeModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        if (!shape.enabled) return;

        shape.radius *= factor;
        shape.donutRadius *= factor;
        shape.length *= factor;
        shape.position *= factor;
        shape.scale *= factor;
        shape.randomPositionAmount *= factor;
    }

    private void ScaleTextureSheetAnimationModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.TextureSheetAnimationModule textureSheet = ps.textureSheetAnimation;
        if (!textureSheet.enabled) return;

        // Không scale frame/sprite sheet. Module này chỉ để giữ chỗ nếu sau này cần mở rộng.
    }

    private void ScaleVelocityOverLifetimeModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        if (!velocity.enabled) return;

        velocity.x = ScaleMinMaxCurve(velocity.x, factor);
        velocity.y = ScaleMinMaxCurve(velocity.y, factor);
        velocity.z = ScaleMinMaxCurve(velocity.z, factor);
        velocity.radial = ScaleMinMaxCurve(velocity.radial, factor);

        velocity.orbitalOffsetX = ScaleMinMaxCurve(velocity.orbitalOffsetX, factor);
        velocity.orbitalOffsetY = ScaleMinMaxCurve(velocity.orbitalOffsetY, factor);
        velocity.orbitalOffsetZ = ScaleMinMaxCurve(velocity.orbitalOffsetZ, factor);
    }

    private void ScaleLimitVelocityOverLifetimeModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
        if (!limit.enabled) return;

        if (limit.separateAxes)
        {
            limit.limitX = ScaleMinMaxCurve(limit.limitX, factor);
            limit.limitY = ScaleMinMaxCurve(limit.limitY, factor);
            limit.limitZ = ScaleMinMaxCurve(limit.limitZ, factor);
        }
        else
        {
            limit.limit = ScaleMinMaxCurve(limit.limit, factor);
        }
    }

    private void ScaleForceOverLifetimeModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
        if (!force.enabled) return;

        force.x = ScaleMinMaxCurve(force.x, factor);
        force.y = ScaleMinMaxCurve(force.y, factor);
        force.z = ScaleMinMaxCurve(force.z, factor);
    }

    private void ScaleNoiseModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.NoiseModule noise = ps.noise;
        if (!noise.enabled) return;

        if (noise.separateAxes)
        {
            noise.strengthX = ScaleMinMaxCurve(noise.strengthX, factor);
            noise.strengthY = ScaleMinMaxCurve(noise.strengthY, factor);
            noise.strengthZ = ScaleMinMaxCurve(noise.strengthZ, factor);
        }
        else
        {
            noise.strength = ScaleMinMaxCurve(noise.strength, factor);
        }
    }

    private void ScaleExternalForcesModule(ParticleSystem ps, float factor)
    {
        ParticleSystem.ExternalForcesModule externalForces = ps.externalForces;
        if (!externalForces.enabled) return;

        externalForces.multiplier *= factor;
    }

    private ParticleSystem.MinMaxCurve ScaleMinMaxCurve(ParticleSystem.MinMaxCurve source, float factor)
    {
        ParticleSystem.MinMaxCurve result = source;

        switch (result.mode)
        {
            case ParticleSystemCurveMode.Constant:
                result.constant *= factor;
                break;

            case ParticleSystemCurveMode.TwoConstants:
                result.constantMin *= factor;
                result.constantMax *= factor;
                break;

            case ParticleSystemCurveMode.Curve:
                result.curveMultiplier *= factor;
                break;

            case ParticleSystemCurveMode.TwoCurves:
                result.curveMultiplier *= factor;
                break;
        }

        return result;
    }
}
#endif
