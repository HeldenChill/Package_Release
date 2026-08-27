using NUnit.Framework;

namespace GrayscaleTextureConverter.Tests
{
    public sealed class GrayscaleTextureSourceDecoderTests
    {
        [Test]
        public void ValidateDimensionsRejectsMismatch()
        {
            bool valid = GrayscaleTextureSourceDecoder.ValidateDimensions(
                actualWidth: 4,
                actualHeight: 2,
                expectedWidth: 8,
                expectedHeight: 2,
                out string error);

            Assert.IsFalse(valid);
            StringAssert.Contains("dimensions", error.ToLowerInvariant());
        }
    }
}
