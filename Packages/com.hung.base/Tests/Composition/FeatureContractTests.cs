using System;
using System.Collections.Generic;
using Hung.Base.Composition;
using NUnit.Framework;

namespace Hung.Base.Tests.Composition
{
    public sealed class FeatureContractTests
    {
        private static FeatureRegistration Create(string id, IReadOnlyList<string> dependencies)
        {
            return new FeatureRegistration(id, true, dependencies,
                () => new FeatureStartResult(true), () => { });
        }

        [Test]
        public void RegistrationCopiesAndProtectsDependencyList()
        {
            var source = new List<string> { "A" };
            FeatureRegistration feature = Create("B", source);
            source[0] = "changed";
            Assert.That(feature.Dependencies[0], Is.EqualTo("A"));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)feature.Dependencies)[0] = "mutated");
        }

        [Test]
        public void InvalidRegistrationInputsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => Create(null, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => Create(" ", Array.Empty<string>()));
            Assert.Throws<ArgumentNullException>(() => Create("A", null));
            Assert.Throws<ArgumentException>(() => Create("A", new[] { "A" }));
            Assert.Throws<ArgumentException>(() => Create("A", new[] { "B", "B" }));
            Assert.Throws<ArgumentException>(() => Create("A", new[] { " " }));
            Assert.Throws<ArgumentNullException>(() => new FeatureRegistration("A", true, Array.Empty<string>(), null, () => { }));
            Assert.Throws<ArgumentNullException>(() => new FeatureRegistration("A", true, Array.Empty<string>(), () => default, null));
        }

        [Test]
        public void IdentifiersAreOrdinalAndDefaultStartResultFails()
        {
            Assert.That(Create("B", new[] { "A", "a" }).Dependencies.Count, Is.EqualTo(2));
            Assert.That(default(FeatureStartResult).Success, Is.False);
        }
    }
}
