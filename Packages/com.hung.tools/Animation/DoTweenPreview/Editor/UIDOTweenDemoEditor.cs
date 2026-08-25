#if UNITY_EDITOR

using DG.Tweening;
using DG.DOTweenEditor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIDOTweenDemo))]
public class UIDOTweenDemoEditor : Editor
{
    private static UIDOTweenDemo activeDemo;
    private static Tween previewTween;
    private static int previewVersion;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UIDOTweenDemo demo = (UIDOTweenDemo)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.HelpBox(
                "Preview animation trực tiếp trong Editor, không cần Play Mode.",
                MessageType.Info
            );

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Current", GUILayout.Height(28)))
                {
                    Undo.RecordObject(demo, "Capture UI Animation Snapshot");
                    demo.CaptureSnapshot();
                    EditorUtility.SetDirty(demo);
                }

                if (GUILayout.Button("Reset", GUILayout.Height(28)))
                {
                    Undo.RecordObject(demo.transform, "Reset UI Animation Snapshot");
                    demo.ResetToSnapshot();
                    EditorUtility.SetDirty(demo);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.65f, 1f, 0.65f);

                if (GUILayout.Button("Preview", GUILayout.Height(32)))
                {
                    StartPreview(demo);
                }

                GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);

                if (GUILayout.Button("Stop", GUILayout.Height(32)))
                {
                    StopPreview(reset: false);
                }

                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button("Stop + Reset", GUILayout.Height(28)))
            {
                StopPreview(reset: true);
            }
        }
    }

    private static void StartPreview(UIDOTweenDemo demo)
    {
        if (demo == null)
        {
            return;
        }

        StopPreview(reset: false);

        activeDemo = demo;
        previewVersion++;

        int currentVersion = previewVersion;

        demo.CaptureSnapshot();

        previewTween = demo.BuildTween();

        if (previewTween == null)
        {
            return;
        }

        previewTween.OnComplete(() =>
        {
            // Delay lại 1 nhịp Editor để tránh kill tween ngay trong lúc DOTween đang update.
            EditorApplication.delayCall += () =>
            {
                if (currentVersion != previewVersion)
                {
                    return;
                }

                StopPreview(reset: false);
            };
        });

        DOTweenEditorPreview.PrepareTweenForPreview(
            previewTween,
            clearCallbacks: false,
            preventAutoKill: true,
            andPlay: true
        );

        DOTweenEditorPreview.Start(() =>
        {
            if (activeDemo != null)
            {
                EditorUtility.SetDirty(activeDemo);
                SceneView.RepaintAll();
            }
        });
    }
    private static void StopPreview(bool reset)
    {
        previewVersion++;

        DOTweenEditorPreview.Stop();

        if (previewTween != null)
        {
            previewTween.Kill();
            previewTween = null;
        }

        if (activeDemo != null && reset)
        {
            activeDemo.ResetToSnapshot();
            EditorUtility.SetDirty(activeDemo);
            SceneView.RepaintAll();
        }

        activeDemo = null;
    }

    private void OnDisable()
    {
        // Không reset tự động khi đổi selection, tránh làm mất trạng thái đang chỉnh.
        DOTweenEditorPreview.Stop();
    }
}

#endif