using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Hung.Cinematics.Tests
{
    [TestFixture]
    public class CinematicCameraTests
    {
        private GameObject _brainGo;
        private Camera _camera;
        private CinemachineBrain _brain;

        private GameObject _captureGo;
        private CinemachineCamera _captureCamera;

        [SetUp]
        public void SetUp()
        {
            _brainGo = new GameObject("TestBrainCamera");
            _camera = _brainGo.AddComponent<Camera>();
            _camera.fieldOfView = 60f;
            _brain = _brainGo.AddComponent<CinemachineBrain>();

            _captureGo = new GameObject("TestCaptureCamera");
            _captureCamera = _captureGo.AddComponent<CinemachineCamera>();
            _captureCamera.enabled = false;
            var lens = _captureCamera.Lens;
            lens.FieldOfView = 50f;
            _captureCamera.Lens = lens;
            _captureGo.transform.position = new Vector3(10, 20, 30);
            _captureGo.transform.rotation = Quaternion.Euler(10, 20, 30);
        }

        [TearDown]
        public void TearDown()
        {
            if (_captureGo != null) Object.DestroyImmediate(_captureGo);
            if (_brainGo != null) Object.DestroyImmediate(_brainGo);
        }

        private CinematicBindings CreateBindings(CinemachineBrain brain = null, CinemachineCamera capture = null)
        {
            return new CinematicBindings
            {
                Brain = brain != null ? brain : _brain,
                CaptureCamera = capture != null ? capture : _captureCamera,
                Frame = Pose.identity
            };
        }

        [Test]
        public void TryAcquire_Succeeds_AndEnablesCaptureCamera()
        {
            var bindings = CreateBindings();
            bool acquired = CinematicCameraLease.TryAcquire(bindings, out var lease, out string diagnostic);
            try
            {
                Assert.IsTrue(acquired, diagnostic);
                Assert.IsNotNull(lease);
                Assert.IsTrue(_captureCamera.enabled);
            }
            finally
            {
                lease?.Dispose();
            }
        }

        [Test]
        public void AcquiringTwiceForSameBrain_RejectsSecondRequest()
        {
            var bindings = CreateBindings();
            bool acquired1 = CinematicCameraLease.TryAcquire(bindings, out var lease1, out string diag1);
            Assert.IsTrue(acquired1, diag1);

            try
            {
                bool acquired2 = CinematicCameraLease.TryAcquire(bindings, out var lease2, out string diag2);
                Assert.IsFalse(acquired2);
                StringAssert.Contains("active", diag2.ToLowerInvariant());
                Assert.IsNull(lease2);
            }
            finally
            {
                lease1?.Dispose();
            }
        }

        [Test]
        public void DisposingFirstLease_PermitsLaterAcquire()
        {
            var bindings = CreateBindings();
            bool acquired1 = CinematicCameraLease.TryAcquire(bindings, out var lease1, out string diag1);
            Assert.IsTrue(acquired1, diag1);
            lease1.Dispose();

            bool acquired2 = CinematicCameraLease.TryAcquire(bindings, out var lease2, out string diag2);
            try
            {
                Assert.IsTrue(acquired2, diag2);
                Assert.IsNotNull(lease2);
            }
            finally
            {
                lease2?.Dispose();
            }
        }

        [Test]
        public void Dispose_RestoresCaptureCameraState_AndDoubleDisposeIsIdempotent()
        {
            var bindings = CreateBindings();
            Vector3 origPos = _captureGo.transform.position;
            Quaternion origRot = _captureGo.transform.rotation;
            float origFov = _captureCamera.Lens.FieldOfView;
            bool origEnabled = _captureCamera.enabled;

            bool acquired = CinematicCameraLease.TryAcquire(bindings, out var lease, out string diag);
            Assert.IsTrue(acquired, diag);

            // Apply different pose during lease
            lease.Apply(new Pose(new Vector3(5, 5, 5), Quaternion.identity), 80f);
            Assert.AreEqual(new Vector3(5, 5, 5), _captureGo.transform.position);
            Assert.AreEqual(80f, _captureCamera.Lens.FieldOfView);

            // First Dispose
            lease.Dispose();
            Assert.AreEqual(origEnabled, _captureCamera.enabled);
            Assert.AreEqual(origPos.x, _captureGo.transform.position.x, 0.001f);
            Assert.AreEqual(origPos.y, _captureGo.transform.position.y, 0.001f);
            Assert.AreEqual(origPos.z, _captureGo.transform.position.z, 0.001f);
            Assert.LessOrEqual(Quaternion.Angle(origRot, _captureGo.transform.rotation), 0.01f);
            Assert.AreEqual(origFov, _captureCamera.Lens.FieldOfView, 0.001f);

            // Second Dispose should not throw or change state
            Assert.DoesNotThrow(() => lease.Dispose());
            Assert.AreEqual(origEnabled, _captureCamera.enabled);
        }

        [Test]
        public void TwoDifferentBrains_DoNotBlockEachOther()
        {
            var brainGo2 = new GameObject("TestBrain2");
            brainGo2.AddComponent<Camera>();
            var brain2 = brainGo2.AddComponent<CinemachineBrain>();

            var captureGo2 = new GameObject("TestCapture2");
            var capture2 = captureGo2.AddComponent<CinemachineCamera>();

            try
            {
                var bindings1 = CreateBindings();
                var bindings2 = CreateBindings(brain2, capture2);

                bool acq1 = CinematicCameraLease.TryAcquire(bindings1, out var lease1, out string diag1);
                bool acq2 = CinematicCameraLease.TryAcquire(bindings2, out var lease2, out string diag2);

                try
                {
                    Assert.IsTrue(acq1, diag1);
                    Assert.IsTrue(acq2, diag2);
                }
                finally
                {
                    lease1?.Dispose();
                    lease2?.Dispose();
                }
            }
            finally
            {
                Object.DestroyImmediate(captureGo2);
                Object.DestroyImmediate(brainGo2);
            }
        }

        [Test]
        public void DestroyedObjects_CleanupIsSafe()
        {
            var bindings = CreateBindings();
            bool acquired = CinematicCameraLease.TryAcquire(bindings, out var lease, out string diag);
            Assert.IsTrue(acquired, diag);

            // Destroy camera and brain GameObjects before disposing
            Object.DestroyImmediate(_captureGo);
            _captureGo = null;
            Object.DestroyImmediate(_brainGo);
            _brainGo = null;

            Assert.DoesNotThrow(() => lease.Dispose());
        }

        [Test]
        public void Reject_WhenCaptureRigIsOutputCameraTransform()
        {
            // Adding CinemachineCamera to the output camera GameObject
            var captureOnBrain = _brainGo.AddComponent<CinemachineCamera>();
            try
            {
                var bindings = CreateBindings(_brain, captureOnBrain);
                bool acquired = CinematicCameraLease.TryAcquire(bindings, out var lease, out string diag);
                Assert.IsFalse(acquired);
                StringAssert.Contains("output", diag.ToLowerInvariant());
                Assert.IsNull(lease);
            }
            finally
            {
                Object.DestroyImmediate(captureOnBrain);
            }
        }
    }
}
