using System;
using System.Linq;
using System.Reflection;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.UI.Tests
{
    public sealed class UISessionConformanceTests
    {
        private UIBackPushPolicy savedPolicy;
        private bool savedClick;
        private object savedObservers;
        private static FieldInfo Observers => typeof(UISCanvas).GetField("AnyOpened", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void Save()
        {
            savedPolicy = UICanvas.BackPushPolicy;
            savedClick = UIButton.DefaultClickSfx;
            savedObservers = Observers.GetValue(null);
        }

        [TearDown]
        public void Restore()
        {
            UICanvas.BackPushPolicy = savedPolicy;
            UIButton.DefaultClickSfx = savedClick;
            Observers.SetValue(null, savedObservers);
        }

        private static MethodInfo[] ResetMethods() => typeof(UIManager).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false)
                .Cast<RuntimeInitializeOnLoadMethodAttribute>()
                .Any(a => a.loadType == RuntimeInitializeLoadType.SubsystemRegistration)).ToArray();

        private static void Reset()
        {
            foreach (MethodInfo method in ResetMethods()) method.Invoke(null, null);
        }

        [Test]
        public void Subsystem_reset_restores_default_back_push_policy()
        {
            UICanvas.BackPushPolicy = UIBackPushPolicy.ExplicitSetup;
            Reset();
            Assert.AreEqual(UIBackPushPolicy.OnOpen, UICanvas.BackPushPolicy);
        }

        [Test]
        public void Subsystem_reset_restores_default_click_sound_policy()
        {
            UIButton.DefaultClickSfx = false;
            Reset();
            Assert.IsTrue(UIButton.DefaultClickSfx);
        }

        [Test]
        public void Subsystem_reset_clears_old_observers_and_allows_one_new_subscription()
        {
            int oldCalls = 0, newCalls = 0;
            Observers.SetValue(null, null);
            UISCanvas.AnyOpened += _ => oldCalls++;
            Reset();
            UISCanvas.AnyOpened += _ => newCalls++;
            ((Action<UISCanvas>)Observers.GetValue(null))?.Invoke(null);
            Assert.AreEqual(0, oldCalls);
            Assert.AreEqual(1, newCalls);
        }

        [Test]
        public void Reset_is_automatic_idempotent_and_precedes_product_policy_reapplication()
        {
            Assert.AreEqual(1, ResetMethods().Length, "Exactly one UI subsystem reset hook must be imported.");
            Reset();
            Reset();
            Assert.AreEqual(UIBackPushPolicy.OnOpen, UICanvas.BackPushPolicy);
            Assert.IsTrue(UIButton.DefaultClickSfx);
            UICanvas.BackPushPolicy = UIBackPushPolicy.ExplicitSetup;
            UIButton.DefaultClickSfx = false;
            Assert.AreEqual(UIBackPushPolicy.ExplicitSetup, UICanvas.BackPushPolicy);
            Assert.IsFalse(UIButton.DefaultClickSfx);
        }
    }
}
