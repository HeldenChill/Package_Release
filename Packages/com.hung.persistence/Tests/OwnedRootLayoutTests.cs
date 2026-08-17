using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Persistence.Tests
{
    [TestFixture]
    public class OwnedRootLayoutTests
    {
        [Test]
        public void DirectoryNames_MatchesTheDirectoriesFileSaveStoreAndEvidenceStoreCreate()
        {
            CollectionAssert.AreEquivalent(
                new[] { "primary", "backup", "quarantine", "receipts" },
                OwnedRootLayout.DirectoryNames);
        }
    }
}
