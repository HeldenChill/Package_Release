using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Hung.Base;

namespace Hung.Base.Tests
{
    // Collider-based CheckTargets must query at the collider's world bounds,
    // not at transform.position + bounds.center (bounds are already world space).
    public class Target3DDetectionTests
    {
        private static readonly Vector3 DetectorPosition = new Vector3(10f, 0f, 5f);
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        private GameObject Make(string name, Vector3 position, int layer)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.position = position;
            created.Add(go);
            return go;
        }

        private Collider SetUp<TCollider>() where TCollider : Collider
        {
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            int defaultLayer = LayerMask.NameToLayer("Default");

            var detector = Make("detector", DetectorPosition, ignoreRaycast);
            var collider = detector.AddComponent<TCollider>();
            if (collider is BoxCollider box) box.size = Vector3.one * 2f;
            if (collider is SphereCollider sphere) sphere.radius = 1f;
            collider.isTrigger = true;
            collider.enabled = false;

            Make("target", DetectorPosition, defaultLayer).AddComponent<BoxCollider>();
            Make("decoy", DetectorPosition * 2f, defaultLayer).AddComponent<BoxCollider>();

            Physics.SyncTransforms();
            return collider;
        }

        private static List<string> Detect(Collider detectCollider)
        {
            var detection = new Target3DDetection<BoxCollider>(new[] { "Default" }, null, 10, detectCollider);
            var targets = new List<BoxCollider>();
            detection.CheckTargets(ref targets);
            var names = new List<string>();
            foreach (var t in targets) names.Add(t.name);
            return names;
        }

        [Test]
        public void BoxCollider_AtNonZeroPosition_HitsTargetAtColliderNotDoubledOffset()
        {
            var names = Detect(SetUp<BoxCollider>());
            CollectionAssert.Contains(names, "target");
            CollectionAssert.DoesNotContain(names, "decoy");
        }

        [Test]
        public void SphereCollider_AtNonZeroPosition_HitsTargetAtColliderNotDoubledOffset()
        {
            var names = Detect(SetUp<SphereCollider>());
            CollectionAssert.Contains(names, "target");
            CollectionAssert.DoesNotContain(names, "decoy");
        }
    }
}
