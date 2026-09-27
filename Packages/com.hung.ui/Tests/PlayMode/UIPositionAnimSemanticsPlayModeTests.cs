using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.UI.PlayModeTests
{
    public sealed class PositionAnimProbe : UIPositionAnim
    {
        public ANIM State => state;
    }

    public sealed class UIPositionAnimSemanticsPlayModeTests
    {
        private GameObject root;
        private Transform moving;
        private PositionAnimProbe probe;
        private int showEnters, hideEnters, showExits, hideExits;

        private void Build(bool waitFrame, float time, bool withHide = true)
        {
            root = new GameObject("PositionRoot");
            var a = new GameObject("A").transform;
            var b = new GameObject("B").transform;
            a.SetParent(root.transform);
            b.SetParent(root.transform);
            a.position = Vector3.zero;
            b.position = new Vector3(100f, 0f, 0f);
            moving = new GameObject("Moving").transform;
            moving.SetParent(root.transform);
            probe = moving.gameObject.AddComponent<PositionAnimProbe>();
#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(probe);
            so.FindProperty("tf").objectReferenceValue = moving;
            so.FindProperty("waitFrame").boolValue = waitFrame;
            UnityEditor.SerializedProperty datas = so.FindProperty("datas");
            datas.arraySize = withHide ? 2 : 1;
            SetData(datas.GetArrayElementAtIndex(0), UIAnim.ANIM.SHOW, a, b, time);
            if (withHide) SetData(datas.GetArrayElementAtIndex(1), UIAnim.ANIM.HIDE, b, a, time);
            so.ApplyModifiedPropertiesWithoutUndo();
#endif
            probe._OnAnimEnter += (_, code) =>
            {
                if (code == (int)UIAnim.ANIM.SHOW) showEnters++;
                if (code == (int)UIAnim.ANIM.HIDE) hideEnters++;
            };
            probe._OnAnimExit += (_, code) =>
            {
                if (code == (int)UIAnim.ANIM.SHOW) showExits++;
                if (code == (int)UIAnim.ANIM.HIDE) hideExits++;
            };
        }

#if UNITY_EDITOR
        private static void SetData(UnityEditor.SerializedProperty p, UIAnim.ANIM id, Transform start, Transform end, float time)
        {
            p.FindPropertyRelative("Id").enumValueIndex = (int)id; // NONE=0, SHOW=1, HIDE=2: index equals value
            p.FindPropertyRelative("Time").floatValue = time;
            p.FindPropertyRelative("StartTf").objectReferenceValue = start;
            p.FindPropertyRelative("EndTf").objectReferenceValue = end;
            p.FindPropertyRelative("IsSetPositionToStart").boolValue = true;
            p.FindPropertyRelative("IsReturnOriginPos").boolValue = false;
        }
#endif

        [SetUp]
        public void Reset() => showEnters = hideEnters = showExits = hideExits = 0;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Without_waitFrame_play_starts_in_the_same_frame()
        {
            Build(waitFrame: false, time: 0.05f);
            probe.Play(UIAnim.ANIM.SHOW);
            Assert.AreEqual(1, showEnters);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WaitFrame_delays_only_the_first_play()
        {
            Build(waitFrame: true, time: 0.05f);
            probe.Play(UIAnim.ANIM.SHOW);
            Assert.AreEqual(0, showEnters, "First play waits.");
            for (int i = 0; i < 8; i++) yield return null;
            Assert.AreEqual(1, showEnters);

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, showExits);
            probe.Play(UIAnim.ANIM.HIDE);
            Assert.AreEqual(1, hideEnters, "Later plays start immediately.");
        }

        [UnityTest]
        public IEnumerator Hide_during_show_reverses_from_current_position_without_snapping()
        {
            Build(waitFrame: false, time: 1f);
            probe.Play(UIAnim.ANIM.SHOW);
            yield return new WaitForSeconds(0.3f);
            float mid = moving.position.x;
            Assert.That(mid, Is.GreaterThan(1f).And.LessThan(99f));

            probe.Play(UIAnim.ANIM.HIDE);
            Assert.AreEqual(UIAnim.ANIM.HIDE, probe.State);
            Assert.AreEqual(mid, moving.position.x, 0.01f, "Reverse must not snap to the HIDE start anchor.");

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, showExits, "Abandoned SHOW never completes.");
            Assert.AreEqual(1, hideExits);
            Assert.AreEqual(0f, moving.position.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator Hide_during_show_without_hide_data_lets_show_finish()
        {
            Build(waitFrame: false, time: 0.5f, withHide: false);
            probe.Play(UIAnim.ANIM.SHOW);
            yield return new WaitForSeconds(0.2f);

            probe.Play(UIAnim.ANIM.HIDE);
            Assert.AreEqual(UIAnim.ANIM.SHOW, probe.State, "No HIDE data: SHOW must not be abandoned.");

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1, showExits);
            Assert.AreEqual(100f, moving.position.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator Repeated_same_request_is_ignored()
        {
            Build(waitFrame: false, time: 0.2f);
            probe.Play(UIAnim.ANIM.SHOW);
            probe.Play(UIAnim.ANIM.SHOW);
            Assert.AreEqual(1, showEnters);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1, showExits);
        }
    }
}
