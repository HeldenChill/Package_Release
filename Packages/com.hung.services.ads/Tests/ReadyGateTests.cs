using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Ads.Tests
{
    public sealed class ReadyGateTests
    {
        [Test]
        public void WarmSdk_RemainsHeldUntilCurrentLaunchAuthorization()
        {
            AdsStartupGate.ResetForPlay();
            try
            {
                AdsStartupGate.Hold();
                int loads = 0;
                var gate = new ReadyGate(() => true && AdsStartupGate.IsAuthorized);
                AdsStartupGate.Authorized += gate.Release;
                gate.Run(() => loads++);
                gate.Release();
                Assert.AreEqual(0, loads, "Warm SDK must not bypass fresh consent.");
                Assert.AreEqual(1, gate.Waiting);
                AdsStartupGate.Authorize();
                AdsStartupGate.Authorize();
                Assert.AreEqual(1, loads);
                Assert.AreEqual(0, gate.Waiting);
            }
            finally { AdsStartupGate.ResetForPlay(); }
        }

        [Test]
        public void AuthorizationBeforeSdkReady_RemainsHeldUntilSdkReady()
        {
            AdsStartupGate.ResetForPlay();
            try
            {
                AdsStartupGate.Hold();
                bool sdkReady = false;
                int loads = 0;
                var gate = new ReadyGate(() => sdkReady && AdsStartupGate.IsAuthorized);
                AdsStartupGate.Authorized += gate.Release;
                gate.Run(() => loads++);
                AdsStartupGate.Authorize();
                Assert.AreEqual(0, loads);
                sdkReady = true;
                gate.Release();
                Assert.AreEqual(1, loads);
                gate.Release();
                Assert.AreEqual(1, loads);
            }
            finally { AdsStartupGate.ResetForPlay(); }
        }

        [Test]
        public void AutomaticGames_AreAuthorizedUnlessTheyOptIn()
        {
            AdsStartupGate.ResetForPlay();
            Assert.IsTrue(AdsStartupGate.IsAuthorized);
        }

        [Test]
        public void NotReady_HoldsActions_ReleaseRunsThemOnceInOrder()
        {
            bool ready = false;
            var ran = new List<string>();
            var gate = new ReadyGate(() => ready);

            gate.Run(() => ran.Add("a"));
            gate.Run(() => ran.Add("b"));
            Assert.IsEmpty(ran);
            Assert.AreEqual(2, gate.Waiting);

            ready = true;
            gate.Release();
            gate.Release();

            CollectionAssert.AreEqual(new[] { "a", "b" }, ran);
            Assert.AreEqual(0, gate.Waiting);
        }

        [Test]
        public void Ready_RunsImmediately()
        {
            var ran = new List<string>();
            var gate = new ReadyGate(() => true);

            gate.Run(() => ran.Add("now"));

            CollectionAssert.AreEqual(new[] { "now" }, ran);
            Assert.AreEqual(0, gate.Waiting);
        }

        [Test]
        public void ThrowingAction_DoesNotStopTheRest()
        {
            var ran = new List<string>();
            bool ready = false;
            var gate = new ReadyGate(() => ready);
            gate.Run(() => throw new InvalidOperationException("boom"));
            gate.Run(() => ran.Add("after"));
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            ready = true;
            gate.Release();

            CollectionAssert.AreEqual(new[] { "after" }, ran);
        }
    }
}
