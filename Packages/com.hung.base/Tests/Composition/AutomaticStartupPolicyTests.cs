using System;
using Hung.Base.Composition;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Base.Tests.Composition
{
    public sealed class AutomaticStartupPolicyTests
    {
        [Test] public void MissingProfile_PreservesBothLegacyDefaults()
        {
            Assert.That(AutomaticStartupPolicy.Evaluate(null, AutomaticStartupKind.LegacyPersistence), Is.True);
            Assert.That(AutomaticStartupPolicy.Evaluate(null, AutomaticStartupKind.LegacyAnalytics), Is.True);
        }
        [Test] public void NewProfile_PreservesBothDefaults()
        {
            var p = ScriptableObject.CreateInstance<AutomaticStartupProfile>();
            try
            { Assert.That(p.legacyPersistence && p.legacyAnalytics, Is.True); }
            finally { UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void Switches_AreIndependent()
        {
            var p = ScriptableObject.CreateInstance<AutomaticStartupProfile>();
            try
            {
                p.legacyPersistence = false;
                Assert.That(AutomaticStartupPolicy.Evaluate(p, AutomaticStartupKind.LegacyPersistence), Is.False);
                Assert.That(AutomaticStartupPolicy.Evaluate(p, AutomaticStartupKind.LegacyAnalytics), Is.True);
                p.legacyPersistence = true; p.legacyAnalytics = false;
                Assert.That(AutomaticStartupPolicy.Evaluate(p, AutomaticStartupKind.LegacyPersistence), Is.True);
                Assert.That(AutomaticStartupPolicy.Evaluate(p, AutomaticStartupKind.LegacyAnalytics), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void UnknownSelector_IsRejectedEvenWithoutProfile()
        { Assert.Throws<ArgumentOutOfRangeException>(() => AutomaticStartupPolicy.Evaluate(null, (AutomaticStartupKind)99)); }
    }
}
