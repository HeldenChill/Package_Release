#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shifts the whole colour tone of a VFX while preserving the relationship between its colours.
///
/// Model: every colour is converted to HSV. The hue is rotated by <c>hueShift</c>, and optionally
/// pulled toward the anchor hue by <c>analogousStrength</c> so the whole effect collapses into an
/// analogous scheme. Saturation / value are only ever scaled by a multiplier, never overwritten,
/// and alpha is never touched - so a bright core stays the brightest thing, a dark rim stays the
/// darkest, and the fade shape of the effect is unchanged.
///
/// Live preview works by binding every colour slot in the object once (a <see cref="ColorSlot"/>
/// holds the ORIGINAL colour plus a setter), then re-writing all slots from those originals on
/// every slider change. Because the shift is always computed from the original, scrubbing is
/// non-destructive and returning a slider to its neutral value restores the effect exactly.
/// </summary>
public class VfxColorToneShifter : EditorWindow
{
    private const string GeneratedMaterialSuffix = "_Tone";
    private const string PreviewRootName = "__VFX_TONE_PREVIEW__";

    /// <summary>
    /// One recolourable colour in the effect: where it came from, and how to write it back.
    /// The original is captured at bind time and never overwritten, so every preview frame
    /// recomputes from the true source instead of compounding on the last preview.
    /// </summary>
    private sealed class ColorSlot
    {
        public Color Original;
        public Action<Color> Setter;
        public UnityEngine.Object Owner;   // for SetDirty / Undo
    }

    /// <summary>
    /// A gradient is bound as a unit: its colour keys are the recolourable part, its alpha keys
    /// are copied verbatim. Binding per-key would let keys drift independently of the curve.
    /// </summary>
    private sealed class GradientSlot
    {
        public GradientColorKey[] OriginalKeys;
        public GradientAlphaKey[] AlphaKeys;
        public GradientMode Mode;
        public Action<Gradient> Setter;
        public UnityEngine.Object Owner;
    }

    /// <summary>
    /// Snapshot of the shift parameters, kept so a live particle's previous shift can be
    /// inverted before the new one is applied (otherwise dragging a slider compounds).
    /// </summary>
    private struct ShiftSettings
    {
        public float HueShift;
        public float SaturationScale;
        public float ValueScale;
        public bool PreserveNearGreys;
        public float GreyThreshold;

        public static ShiftSettings Neutral
        {
            get
            {
                return new ShiftSettings
                {
                    HueShift = 0f,
                    SaturationScale = 1f,
                    ValueScale = 1f,
                    PreserveNearGreys = false,
                    GreyThreshold = 0f
                };
            }
        }
    }

    // --- shift settings -----------------------------------------------------

    [SerializeField] private float hueShift;              // degrees, -180..180
    [SerializeField] private float saturationScale = 1f;
    [SerializeField] private float valueScale = 1f;

    [Tooltip("0 = keep original hue spread. 1 = every hue collapses onto the anchor hue.")]
    [SerializeField] private float analogousStrength;
    [Tooltip("Max degrees a hue may sit away from the anchor when analogous strength is 1.")]
    [SerializeField] private float analogousRange = 30f;

    [SerializeField] private bool includeMaterials = true;
    [SerializeField] private bool includeChildRenderers = true;
    [SerializeField] private bool preserveNearGreys = true;
    [SerializeField] private float greySaturationThreshold = 0.12f;

    // --- state --------------------------------------------------------------

    private readonly List<GameObject> targets = new List<GameObject>();
    private readonly List<ColorSlot> colorSlots = new List<ColorSlot>();
    private readonly List<GradientSlot> gradientSlots = new List<GradientSlot>();

    private Vector2 scroll;
    private float anchorHue;
    private bool anchorHueValid;

    // Live preview
    private GameObject previewInstance;      // temp scene instance, destroyed on exit
    private GameObject previewSourceAsset;   // prefab the preview was spawned from
    private readonly List<Material> previewMaterials = new List<Material>();
    private bool isPreviewing;

    // Off by default: a hand-assembled target list must not be wiped by clicking around the
    // Project window.
    [SerializeField] private bool followSelection;

    private ParticleSystem.Particle[] liveParticleBuffer;
    private ShiftSettings lastAppliedShift = ShiftSettings.Neutral;

    // Editor-driven simulation: the scene view does not tick particles outside play mode, so the
    // preview would sit frozen on its first frame without this.
    private bool autoSimulate = true;
    private double lastSimulationTime;

    [MenuItem("Tools/Universal/Art/Particle/VFX Color Tone Shifter")]
    public static void Open()
    {
        GetWindow<VfxColorToneShifter>("VFX Tone");
    }

    private void OnEnable()
    {
        RefreshTargetsFromSelection();
    }

    private void OnDisable()
    {
        StopPreview();
    }

    private void OnSelectionChange()
    {
        // Don't yank the preview out from under the user mid-tune, and never clobber a list
        // the user assembled by hand - following selection is opt-in.
        if (isPreviewing || !followSelection)
            return;

        RefreshTargetsFromSelection();
        Repaint();
    }

    // =======================================================================
    // GUI
    // =======================================================================

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawTargetSection();
        DrawPreviewControlSection();
        DrawPaletteSection();
        DrawShiftSection();
        DrawApplySection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawTargetSection()
    {
        EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);

        // A live preview is bound to one specific object graph, so changing the target list
        // underneath it would leave the bound slots pointing at the wrong thing.
        bool locked = isPreviewing;

        if (locked)
        {
            EditorGUILayout.HelpBox(
                "Targets are locked while previewing. Cancel the preview to change them.",
                MessageType.None
            );
        }

        using (new EditorGUI.DisabledScope(locked))
        {
            bool listChanged = false;

            for (int i = 0; i < targets.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    GameObject replacement = (GameObject)EditorGUILayout.ObjectField(
                        targets[i], typeof(GameObject), true
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        targets[i] = replacement;
                        listChanged = true;
                    }

                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                    {
                        targets.RemoveAt(i);
                        listChanged = true;
                        i--;
                    }
                }
            }

            // Empty slot at the bottom: assigning it appends a new target.
            EditorGUI.BeginChangeCheck();

            GameObject added = (GameObject)EditorGUILayout.ObjectField(
                "Add", null, typeof(GameObject), true
            );

            if (EditorGUI.EndChangeCheck() && added != null)
            {
                targets.Add(added);
                listChanged = true;
            }

            listChanged |= DrawTargetDropArea();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load From Selection"))
                {
                    RefreshTargetsFromSelection();
                    listChanged = false;   // already rebound inside
                }

                using (new EditorGUI.DisabledScope(targets.Count == 0))
                {
                    if (GUILayout.Button("Clear"))
                    {
                        targets.Clear();
                        listChanged = true;
                    }
                }
            }

            followSelection = EditorGUILayout.Toggle("Follow Selection", followSelection);

            if (listChanged)
            {
                targets.RemoveAll(target => target == null);
                RebindTargets();
            }
        }

        if (targets.Count == 0)
            EditorGUILayout.HelpBox("No target. Drag a VFX prefab or scene object in.", MessageType.Warning);

        DrawUnrecolourableMaterialNotice();

        EditorGUILayout.Space(8f);
    }

    /// <summary>
    /// Tells the user up front which materials the tool will not touch. Without this, a VFX whose
    /// shader has no colour property just appears not to respond, which is easy to read as the
    /// tool being broken rather than the shader tinting from vertex colour.
    /// </summary>
    private void DrawUnrecolourableMaterialNotice()
    {
        if (!includeMaterials || targets.Count == 0)
            return;

        HashSet<string> skipped = new HashSet<string>();

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;

            ParticleSystemRenderer[] renderers =
                targets[i].GetComponentsInChildren<ParticleSystemRenderer>(true);

            for (int r = 0; r < renderers.Length; r++)
            {
                Material material = renderers[r].sharedMaterial;

                if (material != null && !HasRecolourableProperty(material))
                    skipped.Add(material.shader != null ? material.shader.name : material.name);
            }
        }

        if (skipped.Count == 0)
            return;

        EditorGUILayout.HelpBox(
            "These shaders expose no colour property, so their materials are left untouched " +
            "(they tint from the particle's own colour, which IS being shifted):\n  " +
            string.Join("\n  ", new List<string>(skipped).ToArray()),
            MessageType.Info
        );
    }

    /// <summary>
    /// Drop zone accepting several prefabs / scene objects at once. Returns true if anything
    /// was added, so the caller can rebind.
    /// </summary>
    private bool DrawTargetDropArea()
    {
        Rect area = GUILayoutUtility.GetRect(0f, 34f, GUILayout.ExpandWidth(true));

        GUI.Box(area, "Drop VFX prefabs or scene objects here", EditorStyles.helpBox);

        Event evt = Event.current;

        if (!area.Contains(evt.mousePosition))
            return false;

        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
            return false;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (evt.type != EventType.DragPerform)
            return false;

        DragAndDrop.AcceptDrag();

        bool added = false;

        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
        {
            GameObject go = DragAndDrop.objectReferences[i] as GameObject;

            if (go == null || targets.Contains(go))
                continue;

            targets.Add(go);
            added = true;
        }

        evt.Use();
        return added;
    }

    /// <summary>
    /// Rebinds the palette against the current target list without touching Selection - used
    /// when the list is edited by hand rather than driven from the Project window.
    /// </summary>
    private void RebindTargets()
    {
        colorSlots.Clear();
        gradientSlots.Clear();

        for (int i = 0; i < targets.Count; i++)
            BindSlots(targets[i], forPreview: false, bindOnly: true);

        RecalculateAnchorHue();
        Repaint();
    }

    private void DrawPreviewControlSection()
    {
        EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);

        if (!isPreviewing)
        {
            using (new EditorGUI.DisabledScope(targets.Count != 1))
            {
                if (GUILayout.Button("Start Live Preview", GUILayout.Height(28f)))
                    StartPreview();
            }

            EditorGUILayout.HelpBox(
                targets.Count == 1
                    ? "Spawns a temporary preview copy in the scene and updates it as you drag the sliders. Nothing is written to disk until you press Apply."
                    : "Select exactly one VFX to preview. (Apply still works on multiple.)",
                targets.Count == 1 ? MessageType.Info : MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Previewing. Sliders recolour the effect live, including particles already in flight - no restart needed. Press Apply to write the shift to the real asset, or Cancel to discard.",
                MessageType.Info
            );

            autoSimulate = EditorGUILayout.Toggle("Auto Simulate", autoSimulate);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Restart Particles"))
                    RestartPreviewParticles();

                if (GUILayout.Button("Frame In Scene View"))
                    FramePreview();

                if (GUILayout.Button("Cancel Preview"))
                    StopPreview();
            }
        }

        EditorGUILayout.Space(8f);
    }

    private void DrawShiftSection()
    {
        EditorGUILayout.LabelField("Tone Shift", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        hueShift = EditorGUILayout.Slider("Hue Shift", hueShift, -180f, 180f);
        saturationScale = EditorGUILayout.Slider("Saturation Scale", saturationScale, 0f, 2f);
        valueScale = EditorGUILayout.Slider("Value Scale", valueScale, 0f, 2f);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Analogous Collapse", EditorStyles.boldLabel);

        analogousStrength = EditorGUILayout.Slider("Strength", analogousStrength, 0f, 1f);

        using (new EditorGUI.DisabledScope(analogousStrength <= 0f))
        {
            analogousRange = EditorGUILayout.Slider("Range (deg)", analogousRange, 0f, 90f);
        }

        EditorGUILayout.LabelField(" ", "Strength 1 squeezes every hue into +/- Range of the anchor.", EditorStyles.miniLabel);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

        includeMaterials = EditorGUILayout.Toggle("Include Materials", includeMaterials);
        includeChildRenderers = EditorGUILayout.Toggle("Include Child Renderers", includeChildRenderers);
        preserveNearGreys = EditorGUILayout.Toggle("Preserve Near-Greys", preserveNearGreys);

        using (new EditorGUI.DisabledScope(!preserveNearGreys))
        {
            greySaturationThreshold = EditorGUILayout.Slider(
                "Grey Threshold", greySaturationThreshold, 0f, 0.5f
            );
        }

        bool settingsChanged = EditorGUI.EndChangeCheck();

        if (settingsChanged)
        {
            if (isPreviewing)
            {
                ApplyShiftToBoundSlots();
                SceneView.RepaintAll();
            }

            // The palette is drawn ABOVE the sliders, so without this its "After" row would
            // lag one repaint behind the slider the user is dragging.
            Repaint();
        }

        if (GUILayout.Button("Reset Sliders"))
        {
            hueShift = 0f;
            saturationScale = 1f;
            valueScale = 1f;
            analogousStrength = 0f;

            if (isPreviewing)
            {
                ApplyShiftToBoundSlots();
                SceneView.RepaintAll();
            }

            Repaint();
        }

        if (includeMaterials)
        {
            EditorGUILayout.HelpBox(
                "On Apply, materials are cloned to <Prefab>" + GeneratedMaterialSuffix +
                ".mat beside the prefab, so other VFX sharing the original material are not affected.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(8f);
    }

    private void DrawPaletteSection()
    {
        if (colorSlots.Count == 0 && gradientSlots.Count == 0)
            return;

        EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);

        if (anchorHueValid)
            EditorGUILayout.LabelField("Anchor Hue", $"{anchorHue * 360f:0.#} deg");

        List<Color> originals = GetOriginalPalette();

        DrawSwatchRow("Before", originals, false);
        DrawSwatchRow("After", originals, true);

        EditorGUILayout.Space(8f);
    }

    private List<Color> GetOriginalPalette()
    {
        List<Color> palette = new List<Color>();

        for (int i = 0; i < colorSlots.Count; i++)
            palette.Add(colorSlots[i].Original);

        for (int i = 0; i < gradientSlots.Count; i++)
        {
            GradientColorKey[] keys = gradientSlots[i].OriginalKeys;

            for (int k = 0; k < keys.Length; k++)
                palette.Add(keys[k].color);
        }

        return palette;
    }

    private void DrawSwatchRow(string label, List<Color> colors, bool shifted)
    {
        if (colors.Count == 0)
            return;

        EditorGUILayout.LabelField(label);

        Rect row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
        float width = row.width / Mathf.Max(1, colors.Count);

        for (int i = 0; i < colors.Count; i++)
        {
            Color color = shifted ? ShiftColor(colors[i]) : colors[i];
            Rect cell = new Rect(row.x + i * width, row.y, width, row.height);

            EditorGUI.DrawRect(cell, new Color(color.r, color.g, color.b, 1f));
        }
    }

    private void DrawApplySection()
    {
        EditorGUILayout.LabelField("Apply", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(targets.Count == 0))
        {
            if (GUILayout.Button("Apply Tone Shift", GUILayout.Height(34f)))
                ApplyToTargets();
        }

        EditorGUILayout.HelpBox(
            "Apply writes the shifted colours to the real objects. Scene objects are Undo-registered. Prefab assets are saved to disk - use version control to revert.",
            MessageType.Warning
        );
    }

    // =======================================================================
    // Live preview lifecycle
    // =======================================================================

    private void StartPreview()
    {
        StopPreview();

        if (targets.Count != 1 || targets[0] == null)
            return;

        GameObject target = targets[0];
        string assetPath = AssetDatabase.GetAssetPath(target);

        if (!string.IsNullOrEmpty(assetPath))
        {
            // Prefab asset: nothing exists in the scene to look at, so spawn a temp copy.
            previewSourceAsset = target;
            previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(target);
            previewInstance.name = PreviewRootName;

            // Break the prefab link so preview edits can never be applied back by accident.
            PrefabUtility.UnpackPrefabInstance(
                previewInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction
            );

            previewInstance.hideFlags = HideFlags.DontSave;
            PlacePreviewInFrontOfSceneCamera(previewInstance);
        }
        else
        {
            // Scene object: preview it in place, no copy needed.
            previewSourceAsset = null;
            previewInstance = target;
        }

        BindSlots(previewInstance, forPreview: true);
        RecalculateAnchorHue();
        ApplyShiftToBoundSlots();

        isPreviewing = true;
        lastAppliedShift = ShiftSettings.Neutral;

        ApplyShiftToBoundSlots();

        lastSimulationTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;

        FramePreview();
        RestartPreviewParticles();
        SceneView.RepaintAll();
    }

    /// <summary>
    /// Outside play mode nothing advances a ParticleSystem, so the preview would freeze on its
    /// first frame. This drives Simulate at real time so the effect actually plays while tuning.
    /// </summary>
    private void OnEditorUpdate()
    {
        if (!isPreviewing || previewInstance == null)
            return;

        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastSimulationTime);
        lastSimulationTime = now;

        if (!autoSimulate || delta <= 0f)
            return;

        // Cap the step so a stalled editor frame does not fast-forward the whole effect.
        delta = Mathf.Min(delta, 0.1f);

        ParticleSystem[] systems = previewInstance.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];

            // Only drive roots: Simulate cascades to children, and simulating a child directly
            // would double-step it.
            if (ps.transform.parent != null &&
                ps.transform.parent.GetComponentInParent<ParticleSystem>() != null)
            {
                continue;
            }

            ps.Simulate(delta, true, false, true);
        }

        SceneView.RepaintAll();
    }

    private void StopPreview()
    {
        EditorApplication.update -= OnEditorUpdate;
        lastAppliedShift = ShiftSettings.Neutral;

        if (previewInstance != null)
        {
            if (previewSourceAsset != null)
            {
                // Temp copy - just delete it, nothing was ever written to disk.
                DestroyImmediate(previewInstance);
            }
            else
            {
                // Real scene object - put its colours back exactly as they were.
                RestoreBoundSlots();
            }
        }

        for (int i = 0; i < previewMaterials.Count; i++)
        {
            if (previewMaterials[i] != null && !EditorUtility.IsPersistent(previewMaterials[i]))
                DestroyImmediate(previewMaterials[i]);
        }

        previewMaterials.Clear();
        previewInstance = null;
        previewSourceAsset = null;
        isPreviewing = false;

        // Rebind against the existing targets so the palette still works once the preview is
        // gone. Must NOT go through selection - that would wipe a hand-assembled list.
        RebindTargets();
        SceneView.RepaintAll();
    }

    private void RestartPreviewParticles()
    {
        if (previewInstance == null)
            return;

        ParticleSystem[] systems = previewInstance.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            systems[i].Play(true);
        }

        // Particles spawned from here on already carry the CURRENT shift (the module was
        // updated), so the "previous shift to invert" is whatever is on the sliders now.
        lastAppliedShift = new ShiftSettings
        {
            HueShift = hueShift,
            SaturationScale = saturationScale,
            ValueScale = valueScale,
            PreserveNearGreys = preserveNearGreys,
            GreyThreshold = greySaturationThreshold
        };
    }

    private void FramePreview()
    {
        if (previewInstance == null || SceneView.lastActiveSceneView == null)
            return;

        SceneView.lastActiveSceneView.Frame(
            new Bounds(previewInstance.transform.position, Vector3.one * 3f), false
        );
    }

    private static void PlacePreviewInFrontOfSceneCamera(GameObject instance)
    {
        SceneView view = SceneView.lastActiveSceneView;

        instance.transform.position = view != null && view.camera != null
            ? view.camera.transform.position + view.camera.transform.forward * 5f
            : Vector3.zero;
    }

    // =======================================================================
    // Binding - find every colour slot once, remember its original
    // =======================================================================

    private void RefreshTargetsFromSelection()
    {
        targets.Clear();

        UnityEngine.Object[] selection = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable);

        for (int i = 0; i < selection.Length; i++)
        {
            GameObject go = selection[i] as GameObject;

            if (go != null && go != previewInstance)
                targets.Add(go);
        }

        colorSlots.Clear();
        gradientSlots.Clear();

        // Bind read-only (no material cloning) purely to populate the palette swatches.
        for (int i = 0; i < targets.Count; i++)
            BindSlots(targets[i], forPreview: false, bindOnly: true);

        RecalculateAnchorHue();
    }

    /// <param name="forPreview">True when binding the live preview instance: materials are
    /// swapped for throwaway in-memory copies so no .mat asset is ever touched.</param>
    /// <param name="bindOnly">True to only read originals for the palette, never swap materials.</param>
    private void BindSlots(GameObject root, bool forPreview, bool bindOnly = false)
    {
        if (!bindOnly)
        {
            colorSlots.Clear();
            gradientSlots.Clear();
        }

        if (root == null)
            return;

        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];

            BindMinMaxGradient(ps.main.startColor, ps, value =>
            {
                ParticleSystem.MainModule module = ps.main;
                module.startColor = value;
            });

            if (ps.colorOverLifetime.enabled)
            {
                BindMinMaxGradient(ps.colorOverLifetime.color, ps, value =>
                {
                    ParticleSystem.ColorOverLifetimeModule module = ps.colorOverLifetime;
                    module.color = value;
                });
            }

            if (ps.colorBySpeed.enabled)
            {
                BindMinMaxGradient(ps.colorBySpeed.color, ps, value =>
                {
                    ParticleSystem.ColorBySpeedModule module = ps.colorBySpeed;
                    module.color = value;
                });
            }

            if (!includeMaterials)
                continue;

            ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();

            if (psRenderer != null)
                BindMaterial(psRenderer, forPreview, bindOnly);
        }

        if (!includeChildRenderers)
            return;

        SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sprite = sprites[i];
            AddColorSlot(sprite.color, sprite, value => sprite.color = value);
        }

        TrailRenderer[] trails = root.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer trail = trails[i];
            AddGradientSlot(trail.colorGradient, trail, value => trail.colorGradient = value);
        }

        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer line = lines[i];
            AddGradientSlot(line.colorGradient, line, value => line.colorGradient = value);
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            AddColorSlot(light.color, light, value => light.color = value);
        }
    }

    private void BindMinMaxGradient(
        ParticleSystem.MinMaxGradient source,
        UnityEngine.Object owner,
        Action<ParticleSystem.MinMaxGradient> setter)
    {
        switch (source.mode)
        {
            case ParticleSystemGradientMode.Color:
            {
                AddColorSlot(source.color, owner,
                    value => setter(new ParticleSystem.MinMaxGradient(value)));
                break;
            }

            case ParticleSystemGradientMode.TwoColors:
            {
                // Both endpoints must be written together, so bind the pair through one slot
                // each while keeping the other endpoint's original as the partner.
                Color originalMin = source.colorMin;
                Color originalMax = source.colorMax;

                AddColorSlot(originalMin, owner, value =>
                    setter(new ParticleSystem.MinMaxGradient(value, ShiftColor(originalMax))));
                break;
            }

            case ParticleSystemGradientMode.Gradient:
            {
                AddGradientSlot(source.gradient, owner,
                    value => setter(new ParticleSystem.MinMaxGradient(value)));
                break;
            }

            case ParticleSystemGradientMode.TwoGradients:
            {
                Gradient originalMin = CloneGradient(source.gradientMin);
                Gradient originalMax = CloneGradient(source.gradientMax);

                AddGradientSlot(originalMin, owner, value =>
                    setter(new ParticleSystem.MinMaxGradient(value, ShiftGradient(originalMax))));
                break;
            }
        }
    }

    private void BindMaterial(ParticleSystemRenderer psRenderer, bool forPreview, bool bindOnly)
    {
        Material source = psRenderer.sharedMaterial;

        if (source == null)
            return;

        // Shader exposes no colour the tool can drive - skip it entirely rather than swapping
        // the renderer onto a copy that would look identical.
        if (!HasRecolourableProperty(source))
            return;

        string[] properties = GetColorPropertyNames();

        if (bindOnly)
        {
            // Palette-only read: capture the colours, but give the slots a no-op setter so
            // nothing can be written to a shared .mat by accident.
            for (int i = 0; i < properties.Length; i++)
            {
                if (source.HasProperty(properties[i]))
                    AddColorSlot(source.GetColor(properties[i]), source, _ => { });
            }

            return;
        }

        Material writeTarget = source;

        if (forPreview)
        {
            // Throwaway in-memory copy: previewing must never dirty a material asset.
            writeTarget = new Material(source)
            {
                name = source.name + " (Preview)",
                hideFlags = HideFlags.DontSave
            };

            previewMaterials.Add(writeTarget);
            psRenderer.sharedMaterial = writeTarget;
        }

        Material bound = writeTarget;

        for (int i = 0; i < properties.Length; i++)
        {
            if (!bound.HasProperty(properties[i]))
                continue;

            string property = properties[i];
            AddColorSlot(bound.GetColor(property), bound, value => bound.SetColor(property, value));
        }
    }

    private void AddColorSlot(Color original, UnityEngine.Object owner, Action<Color> setter)
    {
        colorSlots.Add(new ColorSlot
        {
            Original = original,
            Setter = setter,
            Owner = owner
        });
    }

    private void AddGradientSlot(Gradient original, UnityEngine.Object owner, Action<Gradient> setter)
    {
        if (original == null)
            return;

        gradientSlots.Add(new GradientSlot
        {
            OriginalKeys = original.colorKeys,
            AlphaKeys = original.alphaKeys,
            Mode = original.mode,
            Setter = setter,
            Owner = owner
        });
    }

    // =======================================================================
    // Writing - always recomputed from the bound originals
    // =======================================================================

    private void ApplyShiftToBoundSlots()
    {
        for (int i = 0; i < colorSlots.Count; i++)
        {
            ColorSlot slot = colorSlots[i];
            slot.Setter(ShiftColor(slot.Original));
        }

        for (int i = 0; i < gradientSlots.Count; i++)
        {
            GradientSlot slot = gradientSlots[i];
            slot.Setter(BuildShiftedGradient(slot));
        }

        if (isPreviewing)
            RecolorLiveParticles();

        // Remember what was just applied so the NEXT change can invert it on live particles.
        lastAppliedShift = new ShiftSettings
        {
            HueShift = hueShift,
            SaturationScale = saturationScale,
            ValueScale = valueScale,
            PreserveNearGreys = preserveNearGreys,
            GreyThreshold = greySaturationThreshold
        };
    }

    /// <summary>
    /// A particle's colour is baked at spawn time from startColor, so editing the module only
    /// affects particles born AFTER the edit - already-alive particles keep the old tone until
    /// they expire. That is why a naive preview looks like it needs a restart.
    ///
    /// This re-derives each live particle's colour: it recovers the particle's original
    /// startColor by dividing out the current lifetime tint, shifts that, then re-applies the
    /// tint. Result: the whole effect recolours in place, mid-flight, no restart.
    /// </summary>
    private void RecolorLiveParticles()
    {
        if (previewInstance == null)
            return;

        ParticleSystem[] systems = previewInstance.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            int alive = ps.particleCount;

            if (alive == 0)
                continue;

            if (liveParticleBuffer == null || liveParticleBuffer.Length < alive)
                liveParticleBuffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(alive)];

            int read = ps.GetParticles(liveParticleBuffer, alive);

            for (int p = 0; p < read; p++)
            {
                Color32 current32 = liveParticleBuffer[p].startColor;
                Color current = current32;

                // startColor on a live particle is the spawn colour, unmodified by
                // colorOverLifetime (Unity applies that at render time). So shifting it
                // directly is correct and needs no un-tinting.
                liveParticleBuffer[p].startColor = ShiftLiveParticleColor(current);
            }

            ps.SetParticles(liveParticleBuffer, read);
        }
    }

    /// <summary>
    /// Live particles carry an ALREADY-SHIFTED colour from the previous slider value, so shifting
    /// them again would compound. Instead the previous shift is inverted first, returning the
    /// particle to its original spawn colour, then the current shift is applied.
    /// </summary>
    private Color ShiftLiveParticleColor(Color current)
    {
        Color original = UnshiftColor(current, lastAppliedShift);
        return ShiftColor(original);
    }

    /// <summary>
    /// Inverse of <see cref="ShiftColor"/> for a given previous setting. Saturation/value scales
    /// are inverted by division; hue by rotating back. The analogous collapse is NOT invertible
    /// (it is lossy by design - it discards hue spread), so when it was active the particle is
    /// left as-is and simply re-tinted by the hue delta difference.
    /// </summary>
    private Color UnshiftColor(Color current, ShiftSettings previous)
    {
        Color.RGBToHSV(current, out float h, out float s, out float v);

        bool wasNearGrey = previous.PreserveNearGreys &&
                           previous.SaturationScale > 0f &&
                           s / previous.SaturationScale <= previous.GreyThreshold;

        if (!wasNearGrey)
            h = Mathf.Repeat(h - previous.HueShift / 360f, 1f);

        if (previous.SaturationScale > 0f)
            s = Mathf.Clamp01(s / previous.SaturationScale);

        if (previous.ValueScale > 0f)
            v = Mathf.Clamp01(v / previous.ValueScale);

        Color result = Color.HSVToRGB(h, s, v, true);
        result.a = current.a;

        return result;
    }

    private void RestoreBoundSlots()
    {
        for (int i = 0; i < colorSlots.Count; i++)
        {
            ColorSlot slot = colorSlots[i];
            slot.Setter(slot.Original);
        }

        for (int i = 0; i < gradientSlots.Count; i++)
        {
            GradientSlot slot = gradientSlots[i];

            Gradient restored = new Gradient { mode = slot.Mode };
            restored.SetKeys(slot.OriginalKeys, slot.AlphaKeys);

            slot.Setter(restored);
        }
    }

    /// <summary>
    /// Rebuilds a gradient with shifted colour keys. Alpha keys are copied verbatim - they are
    /// the fade curve of the effect and must not move.
    /// </summary>
    private Gradient BuildShiftedGradient(GradientSlot slot)
    {
        GradientColorKey[] shifted = new GradientColorKey[slot.OriginalKeys.Length];

        for (int i = 0; i < slot.OriginalKeys.Length; i++)
        {
            shifted[i] = new GradientColorKey(
                ShiftColor(slot.OriginalKeys[i].color),
                slot.OriginalKeys[i].time
            );
        }

        Gradient result = new Gradient { mode = slot.Mode };
        result.SetKeys(shifted, slot.AlphaKeys);

        return result;
    }

    private Gradient ShiftGradient(Gradient source)
    {
        if (source == null)
            return null;

        GradientColorKey[] sourceKeys = source.colorKeys;
        GradientColorKey[] shifted = new GradientColorKey[sourceKeys.Length];

        for (int i = 0; i < sourceKeys.Length; i++)
            shifted[i] = new GradientColorKey(ShiftColor(sourceKeys[i].color), sourceKeys[i].time);

        Gradient result = new Gradient { mode = source.mode };
        result.SetKeys(shifted, source.alphaKeys);

        return result;
    }

    private static Gradient CloneGradient(Gradient source)
    {
        if (source == null)
            return null;

        Gradient clone = new Gradient { mode = source.mode };
        clone.SetKeys(source.colorKeys, source.alphaKeys);

        return clone;
    }

    // =======================================================================
    // Colour maths - the relationship-preserving part
    // =======================================================================

    private Color ShiftColor(Color source)
    {
        Color.RGBToHSV(source, out float h, out float s, out float v);

        // Near-greys (smoke, white sparks) have a meaningless hue - rotating them just adds a
        // colour cast that reads as a bug. Leave their hue alone.
        bool isNearGrey = preserveNearGreys && s <= greySaturationThreshold;

        if (!isNearGrey)
        {
            if (anchorHueValid && analogousStrength > 0f)
                h = CollapseTowardAnchor(h, anchorHue, analogousStrength, analogousRange / 360f);

            h = Mathf.Repeat(h + hueShift / 360f, 1f);
        }

        s = Mathf.Clamp01(s * saturationScale);
        v = Mathf.Clamp01(v * valueScale);

        Color result = Color.HSVToRGB(h, s, v, true);
        result.a = source.a;

        return result;
    }

    /// <summary>
    /// Pulls <paramref name="hue"/> toward <paramref name="anchor"/> so that at strength 1 the
    /// hue sits at most <paramref name="range"/> (normalised) away from the anchor, preserving
    /// the ORDER and SIGN of the original offset. Two hues on opposite sides of the anchor stay
    /// on opposite sides - the spread just narrows.
    /// </summary>
    private static float CollapseTowardAnchor(float hue, float anchor, float strength, float range)
    {
        // Signed shortest offset in -0.5..0.5.
        float offset = Mathf.Repeat(hue - anchor + 0.5f, 1f) - 0.5f;

        // Max possible offset is 0.5, so compressing the whole band by (range / 0.5) maps the
        // widest possible spread onto exactly +/- range. Linear, so hue ORDER is preserved.
        float targetOffset = offset * (range / 0.5f);

        float collapsed = Mathf.Lerp(offset, targetOffset, strength);

        return Mathf.Repeat(anchor + collapsed, 1f);
    }

    /// <summary>
    /// Anchor = the hue of the most saturated, brightest, most opaque colour in the effect. That
    /// is the colour a viewer reads as "the" colour of the VFX, so it is what an analogous scheme
    /// should orbit.
    /// </summary>
    private void RecalculateAnchorHue()
    {
        anchorHueValid = false;

        float bestWeight = 0f;
        List<Color> palette = GetOriginalPalette();

        for (int i = 0; i < palette.Count; i++)
        {
            Color.RGBToHSV(palette[i], out float h, out float s, out float v);

            if (s <= greySaturationThreshold)
                continue;

            float weight = s * v * palette[i].a;

            if (weight <= bestWeight)
                continue;

            bestWeight = weight;
            anchorHue = h;
            anchorHueValid = true;
        }
    }

    private static string[] GetColorPropertyNames()
    {
        return new[] { "_BaseColor", "_Color", "_TintColor", "_EmissionColor" };
    }

    /// <summary>
    /// True if this material actually exposes a colour property the tool would write.
    ///
    /// Many particle shaders - Mobile/Particles/Additive being the common one here - have NO
    /// colour property at all; they tint purely from the particle's own vertex colour. Cloning
    /// such a material produces a byte-identical copy AND rewires the prefab off the shared
    /// material for zero visual gain, which reads as "the material got replaced/lost".
    /// </summary>
    private static bool HasRecolourableProperty(Material material)
    {
        if (material == null)
            return false;

        string[] properties = GetColorPropertyNames();

        for (int i = 0; i < properties.Length; i++)
        {
            if (material.HasProperty(properties[i]))
                return true;
        }

        return false;
    }

    // =======================================================================
    // Apply to the real assets
    // =======================================================================

    private void ApplyToTargets()
    {
        // Snapshot the target list: StopPreview rebuilds it from selection.
        List<GameObject> toApply = new List<GameObject>(targets);

        StopPreview();

        int changed = 0;

        for (int i = 0; i < toApply.Count; i++)
        {
            GameObject target = toApply[i];

            if (target == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(target);

            if (!string.IsNullOrEmpty(assetPath))
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

                CloneMaterialsForApply(contents, assetPath);
                BindSlots(contents, forPreview: false);
                ApplyShiftToBoundSlots();
                MarkBoundOwnersDirty();

                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
                PrefabUtility.UnloadPrefabContents(contents);
            }
            else
            {
                Undo.RegisterFullObjectHierarchyUndo(target, "VFX Tone Shift");

                CloneMaterialsForApply(target, null);
                BindSlots(target, forPreview: false);
                ApplyShiftToBoundSlots();
                MarkBoundOwnersDirty();
            }

            changed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Keep the same targets selected so a second tweak needs no re-dragging.
        targets.Clear();
        targets.AddRange(toApply);
        targets.RemoveAll(target => target == null);

        RebindTargets();
        Repaint();

        Debug.Log($"[VfxColorToneShifter] Applied tone shift to {changed} object(s). Hue {hueShift:0.#} deg, analogous {analogousStrength:0.##}.");
    }

    /// <summary>
    /// Replaces shared materials with prefab-local clones BEFORE binding, so the bound setters
    /// write to the clone. Without this, recolouring one VFX would silently recolour every other
    /// prefab sharing that material.
    /// </summary>
    private void CloneMaterialsForApply(GameObject root, string assetPath)
    {
        if (!includeMaterials)
            return;

        string folder = !string.IsNullOrEmpty(assetPath)
            ? Path.GetDirectoryName(assetPath)
            : "Assets";

        ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        Dictionary<Material, Material> cloneCache = new Dictionary<Material, Material>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Material source = renderers[i].sharedMaterial;

            if (source == null)
                continue;

            // Already a generated tone material for this prefab - write to it directly.
            if (source.name.EndsWith(GeneratedMaterialSuffix))
                continue;

            // Nothing on this material can be recoloured (e.g. Mobile/Particles/Additive tints
            // from vertex colour only). Cloning it would swap the prefab onto a pointless
            // duplicate, so leave the shared material alone.
            if (!HasRecolourableProperty(source))
                continue;

            if (cloneCache.TryGetValue(source, out Material cached))
            {
                renderers[i].sharedMaterial = cached;
                continue;
            }

            string cloneName = root.name + "_" + source.name + GeneratedMaterialSuffix;
            string materialPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(folder, cloneName + ".mat").Replace("\\", "/")
            );

            Material clone = new Material(source) { name = cloneName };
            AssetDatabase.CreateAsset(clone, materialPath);

            cloneCache[source] = clone;
            renderers[i].sharedMaterial = clone;
        }
    }

    private void MarkBoundOwnersDirty()
    {
        for (int i = 0; i < colorSlots.Count; i++)
        {
            if (colorSlots[i].Owner != null)
                EditorUtility.SetDirty(colorSlots[i].Owner);
        }

        for (int i = 0; i < gradientSlots.Count; i++)
        {
            if (gradientSlots[i].Owner != null)
                EditorUtility.SetDirty(gradientSlots[i].Owner);
        }
    }
}
#endif
