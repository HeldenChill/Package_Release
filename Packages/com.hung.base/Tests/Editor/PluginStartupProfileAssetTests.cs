using System;
using Hung.Base.Composition;
using Hung.Base.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Hung.Base.EditorTests
{
    public sealed class PluginStartupProfileAssetTests
    {
        private string folder;
        [SetUp] public void SetUp()
        {
            folder="Assets/__PluginStartupProof_"+Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",folder.Substring("Assets/".Length));
        }
        [TearDown] public void TearDown()
        {
            if(!string.IsNullOrEmpty(folder) && folder.StartsWith("Assets/__PluginStartupProof_",StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(folder);
        }
        private string Create(string leaf,string[] ids)
        {
            AssetDatabase.CreateFolder(folder,leaf); AssetDatabase.CreateFolder(folder+"/"+leaf,"Resources");
            var profile=ScriptableObject.CreateInstance<PluginStartupProfile>();
            var serialized=new SerializedObject(profile); var list=serialized.FindProperty("disabledFeatureIds");
            list.arraySize=ids.Length; for(int i=0;i<ids.Length;i++) list.GetArrayElementAtIndex(i).stringValue=ids[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            string path=folder+"/"+leaf+"/Resources/HungPluginStartup.asset";
            AssetDatabase.CreateAsset(profile,path); AssetDatabase.SaveAssetIfDirty(profile); return path;
        }
        [Test] public void SerializedProfile_RoundTripsItsDisabledId()
        {
            string path=Create("One",new[]{"com.hung.audio"}); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var loaded=AssetDatabase.LoadAssetAtPath<PluginStartupProfile>(path);
            Assert.AreEqual("com.hung.audio",loaded.DisabledFeatureIds[0]); Assert.IsEmpty(loaded.Validate());
        }
        [Test] public void DuplicateResourcesPaths_AreRejected()
        {
            Create("One",Array.Empty<string>()); Create("Two",Array.Empty<string>());
            var error=Assert.Throws<BuildFailedException>(()=>PluginStartupProfileBuildValidator.ValidateProject());
            StringAssert.Contains("Duplicate Resources/HungPluginStartup assets",error.Message);
        }
        [Test] public void InvalidProfile_ReportsItsAssetPath()
        {
            string path=Create("One",new[]{"bad id"});
            var error=Assert.Throws<BuildFailedException>(()=>PluginStartupProfileBuildValidator.ValidateProject());
            StringAssert.Contains(path,error.Message);
        }
    }
}
