using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base.Composition;
using NUnit.Framework;

namespace Hung.Base.Tests.Composition
{
    public sealed class FeatureHostTests
    {
        private sealed class DiagnosticFormattingException : Exception
        {
            public override string Message => throw new InvalidOperationException("message-format-probe");
            public override string ToString() => throw new InvalidOperationException("exception-format-probe");
        }

        private readonly List<string> calls = new List<string>();

        [SetUp]
        public void SetUp() => calls.Clear();

        private FeatureRegistration Feature(string id, bool required = true,
            string[] dependencies = null, Func<FeatureStartResult> start = null, Action stop = null)
        {
            return new FeatureRegistration(id, required, dependencies ?? Array.Empty<string>(),
                () =>
                {
                    calls.Add("start:" + id);
                    return start == null ? new FeatureStartResult(true) : start();
                },
                () =>
                {
                    calls.Add("stop:" + id);
                    stop?.Invoke();
                });
        }

        private static FeatureReport Row(FeatureBootReport report, string id)
            => report.Features.Single(feature => feature.Id == id);

        [Test]
        public void EmptyHostIsReadyWithoutCallbacks()
        {
            using var host = new FeatureHost();
            Assert.That(host.GetReport().ProductReady, Is.False);
            FeatureBootReport report = host.Start();
            Assert.That(report.ProductReady, Is.True);
            Assert.That(report.Features, Is.Empty);
            Assert.That(report.Diagnostics, Is.Empty);
            Assert.That(calls, Is.Empty);
        }

        [Test]
        public void StableTopologicalOrderAndReverseShutdown()
        {
            var host = new FeatureHost();
            host.Register(Feature("B", dependencies: new[] { "A" }));
            host.Register(Feature("A"));
            host.Register(Feature("C"));
            FeatureBootReport startup = host.Start();
            Assert.That(startup.ProductReady, Is.True);
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:B", "start:C" }));
            Assert.That(startup.Features.Select(x => x.Id), Is.EqualTo(new[] { "B", "A", "C" }));
            host.Dispose();
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:B", "start:C", "stop:C", "stop:B", "stop:A" }));
            Assert.That(host.GetReport().ProductReady, Is.False);
            Assert.That(host.GetReport().Features.All(x => x.State == FeatureState.Stopped), Is.True);
            Assert.That(startup.Features.All(x => x.State == FeatureState.Ready), Is.True);
        }

        [Test]
        public void DuplicateRegistrationIsAtomicAndIdsAreOrdinal()
        {
            using var host = new FeatureHost();
            host.Register(Feature("A"));
            Assert.Throws<ArgumentException>(() => host.Register(Feature("A")));
            Assert.Throws<ArgumentNullException>(() => host.Register(null));
            host.Register(Feature("a"));
            Assert.That(host.Start().Features.Count, Is.EqualTo(2));
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:a" }));
            Assert.Throws<KeyNotFoundException>(() => host.GetState("absent"));
        }

        [Test]
        public void MissingRequiredDependencyPreventsAllCallbacks()
        {
            using var host = new FeatureHost();
            host.Register(Feature("independent"));
            host.Register(Feature("B", dependencies: new[] { "missing" }));
            FeatureBootReport report = host.Start();
            Assert.That(report.ProductReady, Is.False);
            Assert.That(Row(report, "B").DiagnosticCode, Is.EqualTo("FEATURE_DEPENDENCY_MISSING"));
            Assert.That(Row(report, "B").DependencyId, Is.EqualTo("missing"));
            Assert.That(host.GetState("independent"), Is.EqualTo(FeatureState.Registered));
            Assert.That(calls, Is.Empty);
        }

        [Test]
        public void MissingOptionalDependencyAllowsIndependentRequiredFeature()
        {
            using var host = new FeatureHost();
            host.Register(Feature("B", false, new[] { "missing" }));
            host.Register(Feature("C"));
            Assert.That(host.Start().ProductReady, Is.True);
            Assert.That(host.GetState("B"), Is.EqualTo(FeatureState.Blocked));
            Assert.That(calls, Is.EqualTo(new[] { "start:C" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CycleMembersAndDownstreamAreDifferent(bool requiredDependent)
        {
            using var host = new FeatureHost();
            host.Register(Feature("A", false, new[] { "B" }));
            host.Register(Feature("B", false, new[] { "A" }));
            host.Register(Feature("D", requiredDependent, new[] { "A" }));
            host.Register(Feature("C"));
            FeatureBootReport report = host.Start();
            Assert.That(Row(report, "A").DiagnosticCode, Is.EqualTo("FEATURE_DEPENDENCY_CYCLE"));
            Assert.That(Row(report, "B").State, Is.EqualTo(FeatureState.Failed));
            Assert.That(Row(report, "D").State, Is.EqualTo(FeatureState.Blocked));
            Assert.That(Row(report, "D").DependencyId, Is.EqualTo("A"));
            Assert.That(report.ProductReady, Is.EqualTo(!requiredDependent));
            Assert.That(calls, Is.EqualTo(requiredDependent ? Array.Empty<string>() : new[] { "start:C" }));
        }

        [Test]
        public void OptionalRuntimeFailureCleansPartialAllocationAndBlocksBranch()
        {
            using var host = new FeatureHost();
            host.Register(Feature("A", false, start: () => new FeatureStartResult(false, "TEST_FAIL")));
            host.Register(Feature("B", false, new[] { "A" }));
            host.Register(Feature("C"));
            FeatureBootReport report = host.Start();
            Assert.That(report.ProductReady, Is.True);
            Assert.That(Row(report, "A").DiagnosticCode, Is.EqualTo("TEST_FAIL"));
            Assert.That(Row(report, "B").DiagnosticCode, Is.EqualTo("FEATURE_DEPENDENCY_NOT_READY"));
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "stop:A", "start:C" }));
        }

        [Test]
        public void RequiredDependentTurnsOptionalFailureFatal()
        {
            using var host = new FeatureHost();
            host.Register(Feature("X"));
            host.Register(Feature("A", false, start: () => default));
            host.Register(Feature("B", true, new[] { "A" }));
            host.Register(Feature("later"));
            Assert.That(host.Start().ProductReady, Is.False);
            Assert.That(host.GetState("B"), Is.EqualTo(FeatureState.Blocked));
            Assert.That(calls, Is.EqualTo(new[] { "start:X", "start:A", "stop:A", "stop:X" }));
        }

        [Test]
        public void RequiredFailureUnwindsInReverseOrderAndDoesNotRetry()
        {
            var host = new FeatureHost();
            host.Register(Feature("A"));
            host.Register(Feature("B"));
            host.Register(Feature("fail", start: () => default));
            host.Register(Feature("later"));
            FeatureBootReport first = host.Start();
            Assert.That(first.ProductReady, Is.False);
            Assert.That(Row(first, "fail").DiagnosticCode, Is.EqualTo("FEATURE_INITIALIZATION_FAILED"));
            Assert.That(host.Start(), Is.SameAs(first));
            host.Dispose();
            host.Dispose();
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:B", "start:fail", "stop:fail", "stop:B", "stop:A" }));
        }

        [Test]
        public void InitializationExceptionRetainsDetails()
        {
            using var host = new FeatureHost();
            host.Register(Feature("A", start: () => throw new InvalidOperationException("init-probe")));
            FeatureBootReport report = host.Start();
            Assert.That(report.ProductReady, Is.False);
            Assert.That(report.Diagnostics.Single().Code, Is.EqualTo("FEATURE_INITIALIZATION_EXCEPTION"));
            StringAssert.Contains("init-probe", report.Diagnostics.Single().ExceptionDetails);
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "stop:A" }));
        }

        [Test]
        public void PartialCleanupFailureIsFatalEvenForOptionalFeature()
        {
            using var host = new FeatureHost();
            host.Register(Feature("X"));
            host.Register(Feature("A", false, start: () => new FeatureStartResult(false, "ORIGINAL"),
                stop: () => throw new InvalidOperationException("cleanup-probe")));
            host.Register(Feature("later"));
            FeatureBootReport report = host.Start();
            Assert.That(report.ProductReady, Is.False);
            Assert.That(Row(report, "A").DiagnosticCode, Is.EqualTo("ORIGINAL"));
            Assert.That(report.Diagnostics.Select(x => x.Code), Does.Contain("FEATURE_SHUTDOWN_EXCEPTION"));
            Assert.That(calls, Is.EqualTo(new[] { "start:X", "start:A", "stop:A", "stop:X" }));
        }

        [Test]
        public void DisposeContinuesAfterCleanupFailureAndExposesCurrentDiagnostics()
        {
            var host = new FeatureHost();
            host.Register(Feature("A"));
            host.Register(Feature("B", stop: () => throw new InvalidOperationException("stop-probe")));
            FeatureBootReport startup = host.Start();
            host.Dispose();
            host.Dispose();
            FeatureBootReport current = host.GetReport();
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:B", "stop:B", "stop:A" }));
            Assert.That(startup.Diagnostics, Is.Empty);
            Assert.That(current.ProductReady, Is.False);
            Assert.That(current.Diagnostics.Single().FeatureId, Is.EqualTo("B"));
            StringAssert.Contains("stop-probe", current.Diagnostics.Single().ExceptionDetails);
        }

        [TestCase("Start")]
        [TestCase("Register")]
        [TestCase("Dispose")]
        public void StartupReentryIsRejectedAndOuterFeatureFails(string operation)
        {
            using var host = new FeatureHost();
            host.Register(Feature("A", start: () =>
            {
                if (operation == "Start") host.Start();
                if (operation == "Register") host.Register(Feature("nested"));
                if (operation == "Dispose") host.Dispose();
                return new FeatureStartResult(true);
            }));
            FeatureBootReport report = host.Start();
            Assert.That(report.ProductReady, Is.False);
            Assert.That(Row(report, "A").State, Is.EqualTo(FeatureState.Failed));
            StringAssert.Contains("InvalidOperationException", report.Diagnostics.Single().ExceptionDetails);
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "stop:A" }));
        }

        [TestCase("Start")]
        [TestCase("Register")]
        [TestCase("Dispose")]
        public void ShutdownReentryIsRecordedWithoutSkippingOtherCleanup(string operation)
        {
            var host = new FeatureHost();
            host.Register(Feature("A"));
            host.Register(Feature("B", stop: () =>
            {
                if (operation == "Start") host.Start();
                if (operation == "Register") host.Register(Feature("nested"));
                if (operation == "Dispose") host.Dispose();
            }));
            host.Start();
            host.Dispose();
            Assert.That(calls.Last(), Is.EqualTo("stop:A"));
            Assert.That(host.GetReport().Diagnostics.Single().Code, Is.EqualTo("FEATURE_SHUTDOWN_EXCEPTION"));
            StringAssert.Contains("InvalidOperationException", host.GetReport().Diagnostics.Single().ExceptionDetails);
        }

        [Test]
        public void RegistrationFreezesAndDisposedOperationsFail()
        {
            var host = new FeatureHost();
            host.Register(Feature("A"));
            FeatureBootReport first = host.Start();
            Assert.That(host.Start(), Is.SameAs(first));
            Assert.Throws<InvalidOperationException>(() => host.Register(Feature("B")));
            Assert.Throws<NotSupportedException>(() => ((IList<FeatureReport>)first.Features).Clear());
            host.Dispose();
            Assert.Throws<ObjectDisposedException>(() => host.Start());
            Assert.Throws<ObjectDisposedException>(() => host.Register(Feature("C")));
            Assert.That(host.GetState("A"), Is.EqualTo(FeatureState.Stopped));
        }

        [Test]
        public void ReadyDependencyDoesNotInventMissingTypedCapability()
        {
            using var host = new FeatureHost();
            IItemSpendService missingSpend = null;
            host.Register(Feature("inventory"));
            host.Register(Feature("shop", dependencies: new[] { "inventory" },
                start: () => new FeatureStartResult(missingSpend != null, "SPEND_CAPABILITY_MISSING")));
            Assert.That(host.Start().ProductReady, Is.False);
            Assert.That(host.GetState("shop"), Is.EqualTo(FeatureState.Failed));
        }

        [Test]
        public void InitializationFormattingFailureStillCleansPartialAndReadyFeatures()
        {
            using var host = new FeatureHost();
            host.Register(Feature("A"));
            host.Register(Feature("B", start: () => throw new DiagnosticFormattingException()));
            FeatureBootReport report = null;

            Assert.DoesNotThrow(() => report = host.Start());

            Assert.That(report.ProductReady, Is.False);
            Assert.That(Row(report, "B").State, Is.EqualTo(FeatureState.Failed));
            Assert.That(Row(report, "B").DiagnosticCode, Is.EqualTo("FEATURE_INITIALIZATION_EXCEPTION"));
            Assert.That(host.GetState("A"), Is.EqualTo(FeatureState.Stopped));
            Assert.That(report.Diagnostics.Single().ExceptionDetails,
                Does.Contain(typeof(DiagnosticFormattingException).FullName));
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:B", "stop:B", "stop:A" }));
            Assert.That(host.Start(), Is.SameAs(report));
            host.Dispose();
            Assert.That(calls.Count, Is.EqualTo(4));
        }

        [Test]
        public void ShutdownFormattingFailureStillDisposesEveryReadyFeatureOnce()
        {
            var host = new FeatureHost();
            host.Register(Feature("A"));
            host.Register(Feature("B", stop: () => throw new DiagnosticFormattingException()));
            Assert.That(host.Start().ProductReady, Is.True);

            Assert.DoesNotThrow(() => host.Dispose());
            Assert.DoesNotThrow(() => host.Dispose());

            FeatureBootReport report = host.GetReport();
            Assert.That(report.ProductReady, Is.False);
            Assert.That(report.Features.All(x => x.State == FeatureState.Stopped), Is.True);
            Assert.That(report.Diagnostics.Single().Code, Is.EqualTo("FEATURE_SHUTDOWN_EXCEPTION"));
            Assert.That(report.Diagnostics.Single().ExceptionDetails,
                Does.Contain(typeof(DiagnosticFormattingException).FullName));
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:B", "stop:B", "stop:A" }));
        }

        [Test]
        public void PartialCleanupFormattingFailureRetainsOriginalFailureAndUnwinds()
        {
            using var host = new FeatureHost();
            host.Register(Feature("A"));
            host.Register(Feature("B", false,
                start: () => new FeatureStartResult(false, "ORIGINAL_FAILURE"),
                stop: () => throw new DiagnosticFormattingException()));
            host.Register(Feature("later"));
            FeatureBootReport report = null;

            Assert.DoesNotThrow(() => report = host.Start());

            Assert.That(report.ProductReady, Is.False);
            Assert.That(Row(report, "B").DiagnosticCode, Is.EqualTo("ORIGINAL_FAILURE"));
            Assert.That(report.Diagnostics.Select(x => x.Code),
                Is.EqualTo(new[] { "ORIGINAL_FAILURE", "FEATURE_SHUTDOWN_EXCEPTION" }));
            Assert.That(report.Diagnostics.Last().ExceptionDetails,
                Does.Contain(typeof(DiagnosticFormattingException).FullName));
            Assert.That(calls, Is.EqualTo(new[] { "start:A", "start:B", "stop:B", "stop:A" }));
        }
    }
}
