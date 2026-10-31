using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Hung.Base.Composition;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public sealed class AutomaticPluginHostTests
    {
        private readonly List<string> calls=new List<string>();
        private static object Invoke(string method)
        {
            try { return typeof(AutomaticPluginHost).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null); }
            catch(TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        private FeatureRegistration Feature(string id,string[] dependencies=null,bool required=false,bool success=true)
            => new FeatureRegistration(id,required,dependencies??Array.Empty<string>(),()=>
            { calls.Add("start:"+id); return new FeatureStartResult(success,"PROBE_FAILED"); },()=>calls.Add("stop:"+id));
        [SetUp] public void SetUp() { Invoke("ResetForPlay"); calls.Clear(); }
        [TearDown] public void TearDown() { Invoke("ResetForPlay"); }
        [Test] public void Collection_DoesNotInitializeAndReportIsUnavailable()
        {
            AutomaticPluginHost.Collect(Feature("a")); Assert.IsEmpty(calls);
            Assert.Throws<InvalidOperationException>(()=>AutomaticPluginHost.GetReport());
        }
        [Test] public void NullDeclaration_Throws() => Assert.Throws<ArgumentNullException>(()=>AutomaticPluginHost.Collect(null));
        [Test] public void SameDeclaration_IsIdempotent()
        {
            var feature=Feature("a"); AutomaticPluginHost.Collect(feature); AutomaticPluginHost.Collect(feature);
            Invoke("StartCollected"); CollectionAssert.AreEqual(new[]{"start:a"},calls);
            Assert.AreEqual(1,AutomaticPluginHost.GetReport().Features.Count);
        }
        [Test] public void DuplicateIds_FailBeforeAnyFeatureStarts()
        {
            AutomaticPluginHost.Collect(Feature("a")); AutomaticPluginHost.Collect(Feature("a")); AutomaticPluginHost.Collect(Feature("z"));
            var error=Assert.Throws<InvalidOperationException>(()=>Invoke("StartCollected"));
            Assert.AreEqual("Duplicate plugin feature IDs: a.",error.Message); Assert.IsEmpty(calls);
            Assert.IsFalse(AutomaticPluginHost.GetReport().ProductReady);
            Assert.AreEqual("PLUGIN_DUPLICATE_IDS",AutomaticPluginHost.GetReport().Diagnostics[0].Code);
        }
        [Test] public void ReversedCollection_StillHonorsDependencies()
        {
            AutomaticPluginHost.Collect(Feature("z",new[]{"a"})); AutomaticPluginHost.Collect(Feature("a"));
            Invoke("StartCollected"); CollectionAssert.AreEqual(new[]{"start:a","start:z"},calls);
        }
        [Test] public void IndependentFeatures_AreOrdinal()
        {
            AutomaticPluginHost.Collect(Feature("z")); AutomaticPluginHost.Collect(Feature("a"));
            Invoke("StartCollected"); CollectionAssert.AreEqual(new[]{"start:a","start:z"},calls);
        }
        [Test] public void MissingRequiredDependency_PreventsInitialization()
        {
            AutomaticPluginHost.Collect(Feature("a",new[]{"missing"},true));
            Invoke("StartCollected"); Assert.IsEmpty(calls); Assert.IsFalse(AutomaticPluginHost.GetReport().ProductReady);
            Assert.AreEqual(FeatureState.Blocked,AutomaticPluginHost.GetReport().Features[0].State);
        }
        [Test] public void Cycle_PreventsBothInitializers()
        {
            AutomaticPluginHost.Collect(Feature("a",new[]{"b"})); AutomaticPluginHost.Collect(Feature("b",new[]{"a"}));
            Invoke("StartCollected"); Assert.IsEmpty(calls);
            foreach(var feature in AutomaticPluginHost.GetReport().Features) Assert.AreEqual(FeatureState.Failed,feature.State);
        }
        [Test] public void OptionalFailure_DoesNotPreventIndependentFeature()
        {
            AutomaticPluginHost.Collect(Feature("a",success:false)); AutomaticPluginHost.Collect(Feature("b"));
            Invoke("StartCollected"); CollectionAssert.AreEqual(new[]{"start:a","stop:a","start:b"},calls);
        }
        [Test] public void RequiredFailure_UnwindsPreviousSuccess()
        {
            AutomaticPluginHost.Collect(Feature("a")); AutomaticPluginHost.Collect(Feature("b",required:true,success:false));
            Invoke("StartCollected"); CollectionAssert.AreEqual(new[]{"start:a","start:b","stop:b","stop:a"},calls);
            Assert.IsFalse(AutomaticPluginHost.GetReport().ProductReady);
        }
        [Test] public void Shutdown_IsReverseAndIdempotent()
        {
            AutomaticPluginHost.Collect(Feature("a")); AutomaticPluginHost.Collect(Feature("b",new[]{"a"}));
            Invoke("StartCollected"); calls.Clear(); AutomaticPluginHost.Shutdown(); AutomaticPluginHost.Shutdown();
            CollectionAssert.AreEqual(new[]{"stop:b","stop:a"},calls); Assert.IsFalse(AutomaticPluginHost.GetReport().ProductReady);
        }
        [Test] public void LateCollection_IsRefused()
        {
            Invoke("StartCollected"); Assert.Throws<InvalidOperationException>(()=>AutomaticPluginHost.Collect(Feature("a")));
        }
        [Test] public void Reset_DisposesOldSessionBeforeClearingItsSlot()
        {
            var marker=new Probe(); bool sawCurrent=false;
            AutomaticPluginHost.Collect(new FeatureRegistration("a",false,Array.Empty<string>(),()=>
            { ServiceSlots.Register<IDisposable>(marker); return new FeatureStartResult(true); },()=>
            { sawCurrent=ReferenceEquals(marker,ServiceSlots.Get<IDisposable>()); ServiceSlots.Unregister<IDisposable>(marker); }));
            Invoke("StartCollected"); Invoke("ResetForPlay");
            Assert.IsTrue(sawCurrent); Assert.IsFalse(ServiceSlots.TryGet<IDisposable>(out _));
            Assert.Throws<InvalidOperationException>(()=>AutomaticPluginHost.GetReport());
        }
        [Test] public void StartingAgain_DoesNotRunCallbacksTwice()
        {
            AutomaticPluginHost.Collect(Feature("a")); Invoke("StartCollected"); Invoke("StartCollected");
            CollectionAssert.AreEqual(new[]{"start:a"},calls);
        }
        private sealed class Probe : IDisposable { public void Dispose() { } }
    }
}
