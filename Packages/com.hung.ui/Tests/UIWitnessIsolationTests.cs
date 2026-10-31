using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace Hung.UI.Tests
{
    public sealed class UIWitnessIsolationTests
    {
        [Test]
        public void Unarmed_repeat_witness_ignores_without_mutating_UI_globals()
        {
            Type type = Type.GetType("Hung.UI.PlayModeTests.UIRepeatPlayWitnessTests, Hung.UI.PlayModeTests", true);
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            string[] names = { "entries", "entryPolicy", "entryClick", "entryStaleObservers" };
            object[] saved = new object[names.Length];
            for (int i = 0; i < names.Length; i++) saved[i] = type.GetField(names[i], flags).GetValue(null);
            var observers = typeof(UISCanvas).GetField("AnyOpened", flags);
            object savedObservers = observers.GetValue(null);
            UIBackPushPolicy policy = UICanvas.BackPushPolicy;
            bool click = UIButton.DefaultClickSfx;
            bool armed = UnityEditor.SessionState.GetBool("Horizon1.UiC0.RepeatArmed", false);
            IEnumerator iterator = null;
            try
            {
                UnityEditor.SessionState.SetBool("Horizon1.UiC0.RepeatArmed", false);
                // Synthetic entry values isolate the guard test; this is not actual play-entry proof.
                type.GetField(names[0], flags).SetValue(null, 1);
                type.GetField(names[1], flags).SetValue(null, UIBackPushPolicy.OnOpen);
                type.GetField(names[2], flags).SetValue(null, true);
                type.GetField(names[3], flags).SetValue(null, 0);
                iterator = (IEnumerator)type.GetMethod("Fresh_entry_resets_UI_globals_before_product_composition")
                    .Invoke(Activator.CreateInstance(type), null);
                Exception ignored = null;
                try { iterator.MoveNext(); }
                catch (Exception error) { ignored = error; }
                Assert.IsNotNull(ignored, "Unarmed witness must explicitly ignore instead of seeding shared state.");
                Assert.AreEqual("NUnit.Framework.IgnoreException", ignored.GetType().FullName);
                Assert.AreEqual(policy, UICanvas.BackPushPolicy);
                Assert.AreEqual(click, UIButton.DefaultClickSfx);
                Assert.AreSame(savedObservers, observers.GetValue(null));
            }
            finally
            {
                (iterator as IDisposable)?.Dispose();
                for (int i = 0; i < names.Length; i++) type.GetField(names[i], flags).SetValue(null, saved[i]);
                UICanvas.BackPushPolicy = policy;
                UIButton.DefaultClickSfx = click;
                observers.SetValue(null, savedObservers);
                UnityEditor.SessionState.SetBool("Horizon1.UiC0.RepeatArmed", armed);
            }
        }
    }
}
