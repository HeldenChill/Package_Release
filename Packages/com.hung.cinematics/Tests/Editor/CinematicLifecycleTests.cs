using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Hung.Cinematics.Tests
{
    [TestFixture]
    public class CinematicLifecycleTests
    {
        private GameObject _playerGo;
        private CinematicPlayer _player;

        private GameObject _brainGo;
        private Camera _camera;
        private CinemachineBrain _brain;

        private GameObject _captureGo;
        private CinemachineCamera _captureCamera;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("TestCinematicPlayer");
            _player = _playerGo.AddComponent<CinematicPlayer>();
            _player.AutoTick = false;

            _brainGo = new GameObject("TestBrain");
            _camera = _brainGo.AddComponent<Camera>();
            _brain = _brainGo.AddComponent<CinemachineBrain>();

            _captureGo = new GameObject("TestCapture");
            _captureCamera = _captureGo.AddComponent<CinemachineCamera>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null) UnityEngine.Object.DestroyImmediate(_playerGo);
            if (_captureGo != null) UnityEngine.Object.DestroyImmediate(_captureGo);
            if (_brainGo != null) UnityEngine.Object.DestroyImmediate(_brainGo);
        }

        private CinematicDefinition CreateDefinition(float duration = 1.0f)
        {
            var def = ScriptableObject.CreateInstance<CinematicDefinition>();
            def.Duration = duration;
            def.CameraKeys = new[]
            {
                new CinematicPoseKey { Time = 0f, Position = Vector3.zero, EulerAngles = Vector3.zero, FieldOfView = 60f },
                new CinematicPoseKey { Time = duration, Position = new Vector3(0, 1, 2), EulerAngles = new Vector3(10, 20, 0), FieldOfView = 50f }
            };
            def.TransformTracks = Array.Empty<CinematicTransformTrack>();
            def.AudioCues = Array.Empty<CinematicAudioCue>();
            return def;
        }

        private CinematicBindings CreateBindings(Dictionary<string, Transform> transforms = null)
        {
            return new CinematicBindings
            {
                Brain = _brain,
                CaptureCamera = _captureCamera,
                Frame = new Pose(new Vector3(1, 0, 1), Quaternion.identity),
                Transforms = transforms ?? new Dictionary<string, Transform>(),
                Audio = new Dictionary<string, AudioSource>()
            };
        }

        [Test]
        public void StandardPlayback_CompletesWithFinalPose()
        {
            var def = CreateDefinition(1.0f);
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out string diag), diag);
                Assert.AreEqual(CinematicState.Prepared, session.State);

                int completions = 0;
                CinematicResult finalResult = default;
                session.Finished += res =>
                {
                    completions++;
                    finalResult = res;
                };

                session.Start();
                Assert.AreEqual(CinematicState.Playing, session.State);

                _player.Tick(0.4f);
                Assert.AreEqual(CinematicState.Playing, session.State);
                Assert.AreEqual(0, completions);

                session.Pause();
                Assert.AreEqual(CinematicState.Paused, session.State);

                _player.Tick(1.0f);
                Assert.AreEqual(CinematicState.Paused, session.State);
                Assert.AreEqual(0, completions);

                session.Resume();
                Assert.AreEqual(CinematicState.Playing, session.State);

                _player.Tick(0.6f);
                Assert.AreEqual(1, completions);
                Assert.AreEqual(CinematicState.Completed, session.State);
                Assert.AreEqual(CinematicState.Completed, finalResult.State);
                Assert.AreEqual(50f, finalResult.FinalFieldOfView, 0.01f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void PreparedSession_CanBeCancelledBeforeStart()
        {
            var def = CreateDefinition();
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                int finishes = 0;
                session.Finished += res =>
                {
                    Assert.AreEqual(CinematicState.Cancelled, res.State);
                    finishes++;
                };

                session.Cancel();
                Assert.AreEqual(CinematicState.Cancelled, session.State);
                Assert.AreEqual(1, finishes);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void Reentrancy_SecondSessionCanPrepareInsideFinished()
        {
            var def = CreateDefinition(0.5f);
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session1, out _));
                bool reentrantAcquireSuccess = false;

                session1.Finished += res =>
                {
                    // In the callback of session1, preparing session2 must succeed and acquire the released camera!
                    reentrantAcquireSuccess = _player.TryPrepare(def, bindings, out var session2, out _);
                    session2?.Dispose();
                };

                session1.Start();
                _player.Tick(0.5f);

                Assert.IsTrue(reentrantAcquireSuccess, "Reentrant TryPrepare failed inside Finished callback.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void NegativeOrNaNDeltaTime_ThrowsArgumentException()
        {
            var def = CreateDefinition();
            var bindings = CreateBindings();
            try
            {
                _player.TryPrepare(def, bindings, out var session, out _);
                session.Start();
                Assert.Throws<ArgumentException>(() => _player.Tick(-0.1f));
                Assert.Throws<ArgumentException>(() => _player.Tick(float.NaN));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void LostRequiredTransform_FailsSession()
        {
            var def = CreateDefinition(1.0f);
            var targetGo = new GameObject("Target");
            def.TransformTracks = new[]
            {
                new CinematicTransformTrack
                {
                    BindingId = "target",
                    Keys = new[]
                    {
                        new CinematicPoseKey { Time = 0f },
                        new CinematicPoseKey { Time = 1f }
                    }
                }
            };

            var transforms = new Dictionary<string, Transform> { { "target", targetGo.transform } };
            var bindings = CreateBindings(transforms);

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                session.Start();
                _player.Tick(0.2f);

                // Destroy target transform during playback
                UnityEngine.Object.DestroyImmediate(targetGo);

                CinematicResult result = default;
                session.Finished += res => result = res;

                _player.Tick(0.2f);
                Assert.AreEqual(CinematicState.Failed, session.State);
                Assert.AreEqual(CinematicState.Failed, result.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void PlayerDisable_CancelsActiveSession()
        {
            var def = CreateDefinition();
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                session.Start();

                CinematicResult result = default;
                session.Finished += res => result = res;

                _player.enabled = false;
                Assert.AreEqual(CinematicState.Cancelled, session.State);
                Assert.AreEqual(CinematicState.Cancelled, result.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void LostCaptureCamera_DuringPlayback_FailsSessionAndPermitsReacquire()
        {
            var def = CreateDefinition(1.0f);
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                session.Start();
                _player.Tick(0.2f);

                int failures = 0;
                int completions = 0;
                session.Finished += res =>
                {
                    if (res.State == CinematicState.Failed) failures++;
                    if (res.State == CinematicState.Completed) completions++;
                };

                UnityEngine.Object.DestroyImmediate(_captureGo);

                _player.Tick(1.0f);

                Assert.AreEqual(1, failures, "Must report Failed exactly once");
                Assert.AreEqual(0, completions, "Must not report Completed");
                Assert.AreEqual(CinematicState.Failed, session.State);

                // Cancel/Dispose after failure must not notify twice
                session.Cancel();
                session.Dispose();
                Assert.AreEqual(1, failures);

                // Assert later player can acquire surviving Brain with replacement camera
                var newCaptureGo = new GameObject("ReplacementCapture");
                try
                {
                    var newCam = newCaptureGo.AddComponent<CinemachineCamera>();
                    var newBindings = new CinematicBindings
                    {
                        Brain = _brain,
                        CaptureCamera = newCam,
                        Frame = new Pose(Vector3.zero, Quaternion.identity),
                        Transforms = new Dictionary<string, Transform>(),
                        Audio = new Dictionary<string, AudioSource>()
                    };
                    Assert.IsTrue(_player.TryPrepare(def, newBindings, out var newSession, out string diag), diag);
                    newSession.Dispose();
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(newCaptureGo);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void LostBrain_DuringPlayback_FailsSession()
        {
            var def = CreateDefinition(1.0f);
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                session.Start();
                _player.Tick(0.2f);

                CinematicResult result = default;
                session.Finished += res => result = res;

                UnityEngine.Object.DestroyImmediate(_brainGo);

                _player.Tick(1.0f);

                Assert.AreEqual(CinematicState.Failed, session.State);
                Assert.AreEqual(CinematicState.Failed, result.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void LostCaptureCamera_BetweenPrepareAndStart_FailsSession()
        {
            var def = CreateDefinition(1.0f);
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                CinematicResult result = default;
                session.Finished += res => result = res;

                UnityEngine.Object.DestroyImmediate(_captureGo);

                session.Start();

                Assert.AreEqual(CinematicState.Failed, session.State);
                Assert.AreEqual(CinematicState.Failed, result.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void LostCaptureCamera_WhilePaused_FailsOnNextTick()
        {
            var def = CreateDefinition(1.0f);
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                session.Start();
                _player.Tick(0.2f);
                session.Pause();

                CinematicResult result = default;
                session.Finished += res => result = res;

                UnityEngine.Object.DestroyImmediate(_captureGo);

                _player.Tick(0.1f);

                Assert.AreEqual(CinematicState.Failed, session.State);
                Assert.AreEqual(CinematicState.Failed, result.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void DisabledCaptureCameraOrBrain_DuringPlayback_FailsSession()
        {
            var def = CreateDefinition(1.0f);
            var bindings = CreateBindings();

            try
            {
                Assert.IsTrue(_player.TryPrepare(def, bindings, out var session, out _));
                session.Start();
                _player.Tick(0.2f);

                CinematicResult result = default;
                session.Finished += res => result = res;

                _captureCamera.enabled = false;

                _player.Tick(0.1f);

                Assert.AreEqual(CinematicState.Failed, session.State);
                Assert.AreEqual(CinematicState.Failed, result.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(def);
            }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void InactiveRenderingObject_FailsOnceAndReleasesCamera(bool deactivateBrain)
        {
            var def = CreateDefinition(1f);
            CinematicSession session = null;
            var inactiveObject = deactivateBrain ? _brainGo : _captureGo;
            try
            {
                Assert.IsTrue(_player.TryPrepare(def, CreateBindings(), out session, out _));
                int notifications = 0;
                CinematicResult result = default;
                session.Finished += value => { notifications++; result = value; };
                session.Start();
                _player.Tick(0.2f);
                inactiveObject.SetActive(false);
                Assert.IsTrue(deactivateBrain ? _brain.enabled : _captureCamera.enabled,
                    "GameObject deactivation does not clear Behaviour.enabled");
                _player.Tick(1.1f);
                Assert.AreEqual(CinematicState.Failed, session.State);
                Assert.AreEqual(CinematicState.Failed, result.State);
                session.Cancel();
                session.Dispose();
                Assert.AreEqual(1, notifications);
                inactiveObject.SetActive(true);
                Assert.IsTrue(_player.TryPrepare(def, CreateBindings(), out var replacement, out string diagnostic), diagnostic);
                replacement.Dispose();
            }
            finally
            {
                session?.Dispose();
                inactiveObject.SetActive(true);
                UnityEngine.Object.DestroyImmediate(def);
            }
        }
    }
}
