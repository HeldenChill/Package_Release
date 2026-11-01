using System;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Cinematics.Tests
{
    [TestFixture]
    public class CinematicDefinitionTests
    {
        private CinematicDefinition CreateValidDefinition()
        {
            var def = ScriptableObject.CreateInstance<CinematicDefinition>();
            def.Duration = 2.5f;
            def.CameraKeys = new[]
            {
                new CinematicPoseKey { Time = 0f, Position = Vector3.zero, EulerAngles = Vector3.zero, FieldOfView = 60f },
                new CinematicPoseKey { Time = 2.5f, Position = Vector3.forward, EulerAngles = Vector3.zero, FieldOfView = 60f }
            };
            def.TransformTracks = Array.Empty<CinematicTransformTrack>();
            def.AudioCues = Array.Empty<CinematicAudioCue>();
            return def;
        }

        [Test]
        public void ValidDefinition_PassesValidation()
        {
            var def = CreateValidDefinition();
            try
            {
                Assert.IsTrue(def.Validate(out string diagnostic), diagnostic);
                Assert.IsNull(diagnostic);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void EmptyCameraTrack_IsRejected()
        {
            var definition = ScriptableObject.CreateInstance<CinematicDefinition>();
            try
            {
                definition.Duration = 2.5f;
                definition.CameraKeys = Array.Empty<CinematicPoseKey>();
                Assert.IsFalse(definition.Validate(out string diagnostic));
                StringAssert.Contains("camera", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidDuration_IsRejected(float duration)
        {
            var def = CreateValidDefinition();
            try
            {
                def.Duration = duration;
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("duration", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void MissingFirstCameraKeyAtZero_IsRejected()
        {
            var def = CreateValidDefinition();
            try
            {
                def.CameraKeys[0] = new CinematicPoseKey { Time = 0.1f, FieldOfView = 60f };
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("zero", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void MissingLastCameraKeyAtDuration_IsRejected()
        {
            var def = CreateValidDefinition();
            try
            {
                def.CameraKeys[1] = new CinematicPoseKey { Time = 2.0f, FieldOfView = 60f };
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("duration", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void DescendingCameraKeyTimes_IsRejected()
        {
            var def = CreateValidDefinition();
            try
            {
                def.CameraKeys = new[]
                {
                    new CinematicPoseKey { Time = 0f, FieldOfView = 60f },
                    new CinematicPoseKey { Time = 1.5f, FieldOfView = 60f },
                    new CinematicPoseKey { Time = 1.0f, FieldOfView = 60f },
                    new CinematicPoseKey { Time = 2.5f, FieldOfView = 60f }
                };
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("descending", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [TestCase(0.5f)]
        [TestCase(180f)]
        [TestCase(float.NaN)]
        public void CameraFovOutsideRange_IsRejected(float fov)
        {
            var def = CreateValidDefinition();
            try
            {
                def.CameraKeys[0] = new CinematicPoseKey { Time = 0f, FieldOfView = fov };
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("fov", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void NonFiniteCameraPose_IsRejected()
        {
            var def = CreateValidDefinition();
            try
            {
                def.CameraKeys[0] = new CinematicPoseKey { Time = 0f, Position = new Vector3(float.NaN, 0, 0), FieldOfView = 60f };
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("finite", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void EmptyTransformTrackBindingId_IsRejected(string bindingId)
        {
            var def = CreateValidDefinition();
            try
            {
                def.TransformTracks = new[]
                {
                    new CinematicTransformTrack
                    {
                        BindingId = bindingId,
                        Keys = new[] { new CinematicPoseKey { Time = 0f } }
                    }
                };
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("binding", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void DuplicateTransformTrackBindingId_IsRejected()
        {
            var def = CreateValidDefinition();
            try
            {
                def.TransformTracks = new[]
                {
                    new CinematicTransformTrack
                    {
                        BindingId = "target",
                        Keys = new[] { new CinematicPoseKey { Time = 0f } }
                    },
                    new CinematicTransformTrack
                    {
                        BindingId = "target",
                        Keys = new[] { new CinematicPoseKey { Time = 0f } }
                    }
                };
                Assert.IsFalse(def.Validate(out string diagnostic));
                StringAssert.Contains("duplicate", diagnostic.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }
    }
}
