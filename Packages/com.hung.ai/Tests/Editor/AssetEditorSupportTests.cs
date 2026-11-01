using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AssetEditorSupportTests
    {
        AIGraphAsset asset;

        [SetUp]
        public void SetUp()
        {
            asset = ScriptableObject.CreateInstance<AIGraphAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void SerializedEdit_RebuildsNodeLookup()
        {
            asset.AddNode(new WaitNode { Id = "a" });
            Assert.IsNotNull(asset.GetNode("a"), "builds the lookup");
            var so = new SerializedObject(asset);
            so.FindProperty("nodes.Array.data[0].id").stringValue = "b";
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsNull(asset.GetNode("a"));
            Assert.IsNotNull(asset.GetNode("b"));
        }

        [Test]
        public void RemoveMissingNodes_DropsNullEntries()
        {
            asset.AddNode(new WaitNode { Id = "a" });
            asset.AddNode<AINode>(null);
            Assert.AreEqual(1, asset.RemoveMissingNodes());
            Assert.AreEqual(1, asset.Nodes.Count);
            Assert.IsNotNull(asset.GetNode("a"));
        }

        [Test]
        public void LayoutLists_AreCopiedIntoRuntimeCopy()
        {
            asset.Groups.Add(new AIGraphGroup { Title = "G" });
            asset.Notes.Add(new AIGraphNote { Contents = "hello" });
            AIGraphAsset copy = asset.CreateRuntimeCopy();
            try
            {
                Assert.AreEqual(1, copy.Groups.Count);
                Assert.AreEqual("G", copy.Groups[0].Title);
                Assert.AreEqual("hello", copy.Notes[0].Contents);
            }
            finally { Object.DestroyImmediate(copy); }
        }
    }
}
