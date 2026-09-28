using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hung.UI.Tests
{
    /// <summary>PVM absorption parity (0.7.1): UIAlphaAnim loops IDLE, Stop releases the running
    /// animation, and UIScaleAnim keyframes are readable by product code.</summary>
    public class UIAnimPvmParityTests
    {
        private GameObject gameObject;
        private CanvasGroup group;
        private UIAlphaAnim alpha;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("AlphaParity", typeof(CanvasGroup));
            group = gameObject.GetComponent<CanvasGroup>();
            alpha = gameObject.AddComponent<UIAlphaAnim>();
            var serialized = new SerializedObject(alpha);
            serialized.FindProperty("canvasGroup").objectReferenceValue = group;
            SerializedProperty datas = serialized.FindProperty("datas");
            datas.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                SerializedProperty data = datas.GetArrayElementAtIndex(i);
                data.FindPropertyRelative("Id").enumValueIndex = i + 1; // SHOW, HIDE, IDLE
                data.FindPropertyRelative("Time").floatValue = 0.3f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            alpha.Stop();
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Idle_plays_and_announces_itself()
        {
            int entered = -1;
            alpha._OnAnimEnter += (id, anim) => entered = anim;
            alpha.Play(UIAnim.ANIM.IDLE);
            Assert.AreEqual((int)UIAnim.ANIM.IDLE, entered);
        }

        [Test]
        public void Stop_releases_the_running_animation()
        {
            alpha.Play(UIAnim.ANIM.SHOW);
            alpha.Stop();
            group.alpha = 1f;
            alpha.Play(UIAnim.ANIM.SHOW);
            Assert.AreEqual(0f, group.alpha, "SHOW restarts from alpha 0 once Stop released the previous run");
        }

        [Test]
        public void Scale_keyframes_are_public_for_product_readers()
        {
            System.Type properties = typeof(UIScaleAnim).GetNestedType("Propertys", BindingFlags.Public);
            Assert.IsNotNull(properties);
            Assert.IsNotNull(properties.GetField("StartSize"));
        }
    }
}
