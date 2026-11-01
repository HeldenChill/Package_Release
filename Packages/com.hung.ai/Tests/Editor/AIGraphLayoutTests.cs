using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AIGraphLayoutTests
    {
        [Test]
        public void Compute_CentersParentsOverChildren()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                var root = asset.AddNode(new SelectorNode { Id = "root" });
                var a = asset.AddNode(new SequenceNode { Id = "a" });
                asset.AddNode(new WaitNode { Id = "c1" });
                asset.AddNode(new WaitNode { Id = "c2" });
                asset.AddNode(new WaitNode { Id = "b" });
                root.AddChild("a");
                root.AddChild("b");
                a.AddChild("c1");
                a.AddChild("c2");
                asset.RootNodeId = "root";
                Dictionary<string, Vector2> p = AIGraphLayout.Compute(asset);
                Assert.AreEqual(5, p.Count);
                Assert.AreEqual(new Vector2(0f, 220f), p["c1"]);
                Assert.AreEqual(new Vector2(198f, 220f), p["c2"]);
                Assert.AreEqual(new Vector2(99f, 110f), p["a"]);
                Assert.AreEqual(new Vector2(396f, 110f), p["b"]);
                Assert.AreEqual(new Vector2(247.5f, 0f), p["root"]);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Compute_PlacesOrphansToTheRight()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                var root = asset.AddNode(new SequenceNode { Id = "root" });
                asset.AddNode(new WaitNode { Id = "w" });
                var orphan = asset.AddNode(new SequenceNode { Id = "orphan" });
                asset.AddNode(new WaitNode { Id = "ow" });
                root.AddChild("w");
                orphan.AddChild("ow");
                asset.RootNodeId = "root";
                Dictionary<string, Vector2> p = AIGraphLayout.Compute(asset);
                Assert.AreEqual(4, p.Count);
                Assert.AreEqual(new Vector2(0f, 110f), p["w"]);
                Assert.AreEqual(new Vector2(198f, 110f), p["ow"]);
                Assert.AreEqual(new Vector2(198f, 0f), p["orphan"]);
                asset.RootNodeId = null;
                Assert.AreEqual(4, AIGraphLayout.Compute(asset).Count);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
