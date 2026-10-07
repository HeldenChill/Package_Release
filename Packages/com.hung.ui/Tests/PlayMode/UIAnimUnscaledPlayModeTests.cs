using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.UI.PlayModeTests
{
    public sealed class UIAnimUnscaledPlayModeTests
    {
        private GameObject root;
        private float savedScale;

        [SetUp]
        public void SetUp() => savedScale = Time.timeScale;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = savedScale;
            if (root != null) Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Position_anim_completes_while_timeScale_is_zero()
        {
            root = new GameObject("PositionRoot");
            var a = new GameObject("A").transform;
            var b = new GameObject("B").transform;
            a.SetParent(root.transform);
            b.SetParent(root.transform);
            b.position = new Vector3(100f, 0f, 0f);
            var moving = new GameObject("Moving").transform;
            moving.SetParent(root.transform);
            var probe = moving.gameObject.AddComponent<PositionAnimProbe>();
#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(probe);
            so.FindProperty("tf").objectReferenceValue = moving;
            UnityEditor.SerializedProperty datas = so.FindProperty("datas");
            datas.arraySize = 1;
            UnityEditor.SerializedProperty p = datas.GetArrayElementAtIndex(0);
            p.FindPropertyRelative("Id").enumValueIndex = (int)UIAnim.ANIM.SHOW;
            p.FindPropertyRelative("Time").floatValue = 0.1f;
            p.FindPropertyRelative("StartTf").objectReferenceValue = a;
            p.FindPropertyRelative("EndTf").objectReferenceValue = b;
            p.FindPropertyRelative("IsSetPositionToStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
#endif
            int exits = 0;
            probe._OnAnimExit += (_, code) => { if (code == (int)UIAnim.ANIM.SHOW) exits++; };

            Time.timeScale = 0f;
            probe.Play(UIAnim.ANIM.SHOW);
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.AreEqual(1, exits, "SHOW must finish on real time while the world is frozen.");
            Assert.AreEqual(100f, moving.position.x, 0.01f);
        }
    }
}
