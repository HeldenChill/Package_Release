using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base.Composition;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Base.Tests
{
    public sealed class PluginStartupPolicyTests
    {
        private PluginStartupProfile profile;
        private static void Reset()=>typeof(PluginStartupPolicy).GetMethod("ResetSession",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        private void SetIds(params string[] ids)=>typeof(PluginStartupProfile).GetField("disabledFeatureIds",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(profile,new List<string>(ids));
        [SetUp] public void SetUp() { Reset(); profile=ScriptableObject.CreateInstance<PluginStartupProfile>(); }
        [TearDown] public void TearDown() { Reset(); UnityEngine.Object.DestroyImmediate(profile); }
        [Test] public void AbsentAndEmpty_EnableFeature()
        {
            Assert.IsTrue(PluginStartupPolicy.Evaluate(null,"com.hung.audio"));
            Assert.IsTrue(PluginStartupPolicy.Evaluate(profile,"com.hung.audio"));
        }
        [Test] public void DisabledId_SuppressesOnlyThatFeature()
        {
            SetIds("com.hung.audio"); Assert.IsFalse(PluginStartupPolicy.Evaluate(profile,"com.hung.audio"));
            Assert.IsTrue(PluginStartupPolicy.Evaluate(profile,"com.hung.ui"));
        }
        [TestCase("")][TestCase(" ")][TestCase("Com.Hung.Audio")][TestCase("com..audio")][TestCase("com.hung.audio ")]
        public void MalformedId_IsRejected(string id)
        {
            SetIds(id); Assert.IsNotEmpty(profile.Validate());
            Assert.Throws<InvalidOperationException>(()=>PluginStartupPolicy.Evaluate(profile,"com.hung.ui"));
        }
        [Test] public void DuplicateIds_AreRejected()
        {
            SetIds("com.hung.audio","com.hung.audio"); Assert.AreEqual(1,profile.Validate().Count);
            Assert.Throws<InvalidOperationException>(()=>PluginStartupPolicy.Evaluate(profile,"com.hung.ui"));
        }
        [Test] public void InvalidQuery_Throws()=>Assert.Throws<ArgumentException>(()=>PluginStartupPolicy.Evaluate(null,"bad id"));
        [Test] public void ScopedOverrides_RestoreAfterOutOfOrderDisposal()
        {
            bool baseline=PluginStartupPolicy.ShouldStart("com.hung.audio");
            var first=PluginStartupPolicy.OverrideForTests("com.hung.audio",false);
            var second=PluginStartupPolicy.OverrideForTests("com.hung.audio",true);
            try { first.Dispose(); Assert.IsTrue(PluginStartupPolicy.ShouldStart("com.hung.audio")); }
            finally { second.Dispose(); first.Dispose(); }
            Assert.AreEqual(baseline,PluginStartupPolicy.ShouldStart("com.hung.audio"));
        }
        [Test] public void OldScopeCannotMutateNewSession()
        {
            var old=PluginStartupPolicy.OverrideForTests("com.hung.audio",false); Reset();
            using(PluginStartupPolicy.OverrideForTests("com.hung.audio",true))
            { old.Dispose(); Assert.IsTrue(PluginStartupPolicy.ShouldStart("com.hung.audio")); }
        }

    }
}
