#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using DG.DOTweenEditor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Previews DOTween animations that are built entirely from code (no DOTweenAnimation
/// component) by instantiating a prefab in Edit Mode, invoking a chosen public method via
/// reflection, and driving any tweens/sequences that method starts through DOTweenEditorPreview.
/// Unlike com.hung.tools' UIDOTweenDemo (fixed presets only), this calls the prefab's real
/// methods, so it reproduces the actual gameplay animation, not an approximation.
/// </summary>
public class MethodTweenPreviewWindow : EditorWindow
{
    GameObject sourcePrefab;
    GameObject previewInstance;
    Vector3 spawnPosition = new(0f, 3f, 0f);

    Component selectedComponent;
    MethodInfo selectedMethod;
    Vector2 scroll;

    static readonly Dictionary<Component, ComponentSnapshot> Snapshots = new();

    [MenuItem("Tools/Universal/Preview/Method Tween Preview")]
    static void Open()
    {
        GetWindow<MethodTweenPreviewWindow>("Method Tween Preview");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Preview DOTween animations driven by plain code (DOScale/DOFade/DOMove calls " +
            "inside a method), not just DOTweenAnimation components. Spawns the prefab, lets " +
            "you invoke any public method, and scrubs whatever tweens that call starts.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        var newPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", sourcePrefab, typeof(GameObject), false);
        if (newPrefab != sourcePrefab)
        {
            DestroyPreviewInstance();
            sourcePrefab = newPrefab;
        }

        spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", spawnPosition);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = sourcePrefab != null;
            if (GUILayout.Button("Spawn / Respawn", GUILayout.Height(24)))
            {
                SpawnPreviewInstance();
            }
            GUI.enabled = previewInstance != null;
            if (GUILayout.Button("Despawn", GUILayout.Height(24)))
            {
                DestroyPreviewInstance();
            }
            GUI.enabled = true;
        }

        if (previewInstance == null)
        {
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Method To Invoke", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(220));
        foreach (var comp in previewInstance.GetComponentsInChildren<Component>(true))
        {
            if (comp == null || comp is Transform) continue;

            var methods = GetInvokableMethods(comp);
            if (methods.Count == 0) continue;

            EditorGUILayout.LabelField(comp.GetType().Name, EditorStyles.miniBoldLabel);
            foreach (var m in methods)
            {
                bool isSelected = selectedComponent == comp && selectedMethod == m;
                bool nowSelected = EditorGUILayout.ToggleLeft("  " + FormatMethodSignature(m), isSelected);
                if (nowSelected && !isSelected)
                {
                    selectedComponent = comp;
                    selectedMethod = m;
                }
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.65f, 1f, 0.65f);
            GUI.enabled = selectedMethod != null;
            if (GUILayout.Button("Capture + Play", GUILayout.Height(30)))
            {
                CaptureSnapshot(previewInstance);
                InvokeSelected();
                DOTweenEditorPreview.Start(() => SceneView.RepaintAll());
            }
            GUI.enabled = true;

            GUI.backgroundColor = new Color(1f, 0.8f, 0.65f);
            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                DOTweenEditorPreview.Stop();
            }

            GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);
            if (GUILayout.Button("Stop + Reset", GUILayout.Height(30)))
            {
                DOTweenEditorPreview.Stop();
                RestoreSnapshot(previewInstance);
            }
            GUI.backgroundColor = Color.white;
        }
    }

    static List<MethodInfo> GetInvokableMethods(Component comp)
    {
        return comp.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().All(p => p.HasDefaultValue))
            .Where(m => !m.IsSpecialName)
            .Where(m => m.DeclaringType != typeof(UnityEngine.Object) && m.DeclaringType != typeof(Component) && m.DeclaringType != typeof(Behaviour) && m.DeclaringType != typeof(MonoBehaviour))
            .GroupBy(m => m.Name)
            .Select(g => g.First())
            .OrderBy(m => m.Name)
            .ToList();
    }

    static string FormatMethodSignature(MethodInfo m)
    {
        var pars = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name + (p.HasDefaultValue ? " = " + (p.DefaultValue ?? "null") : "")));
        return m.Name + "(" + pars + ")";
    }

    void InvokeSelected()
    {
        if (selectedComponent == null || selectedMethod == null) return;

        var pars = selectedMethod.GetParameters();
        var args = new object[pars.Length];
        for (int i = 0; i < pars.Length; i++)
        {
            args[i] = pars[i].HasDefaultValue ? pars[i].DefaultValue : GetDefault(pars[i].ParameterType);
        }

        try
        {
            selectedMethod.Invoke(selectedComponent, args);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    static object GetDefault(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

    void SpawnPreviewInstance()
    {
        DestroyPreviewInstance();
        if (sourcePrefab == null) return;

        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
        previewInstance.transform.position = spawnPosition;
        previewInstance.hideFlags = HideFlags.DontSave;
        Selection.activeGameObject = previewInstance;
        SceneView.FrameLastActiveSceneView();
    }

    void DestroyPreviewInstance()
    {
        DOTweenEditorPreview.Stop();
        Snapshots.Clear();
        selectedComponent = null;
        selectedMethod = null;
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }
        previewInstance = null;
    }

    static void CaptureSnapshot(GameObject root)
    {
        Snapshots.Clear();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            Snapshots[t] = new ComponentSnapshot
            {
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale
            };
        }
        foreach (var g in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            Snapshots[g] = new ComponentSnapshot { color = g.color };
        }
        foreach (var tmp in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            Snapshots[tmp] = new ComponentSnapshot { color = tmp.color, alpha = tmp.alpha };
        }
    }

    static void RestoreSnapshot(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!Snapshots.TryGetValue(t, out var s)) continue;
            t.localPosition = s.localPosition;
            t.localRotation = s.localRotation;
            t.localScale = s.localScale;
        }
        foreach (var g in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            if (Snapshots.TryGetValue(g, out var s)) g.color = s.color;
        }
        foreach (var tmp in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (!Snapshots.TryGetValue(tmp, out var s)) continue;
            tmp.color = s.color;
            tmp.alpha = s.alpha;
        }
    }

    void OnDestroy()
    {
        DestroyPreviewInstance();
    }

    struct ComponentSnapshot
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Color color;
        public float alpha;
    }
}
#endif
