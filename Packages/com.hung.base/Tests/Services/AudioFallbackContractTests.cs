using Hung.Base;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public sealed class AudioFallbackContractTests
    {
        [TearDown] public void TearDown() => ServiceSlots.ResetForTests();
        [Test] public void LookupWithoutOwnerIsNonNullAndDoesNotClaimOwnership()
        {
            ServiceSlots.ResetForTests();
            Assert.That(Locator.Audio, Is.Not.Null);
            Assert.That(ServiceSlots.TryGet<IAudioService>(out _), Is.False);
            Assert.That(ServiceSlots.OwnerOf<IAudioService>(), Is.Null);
        }
    }
}
