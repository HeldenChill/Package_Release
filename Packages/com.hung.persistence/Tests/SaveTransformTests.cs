using System;
using System.IO;
using System.Linq;
using System.Text;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class SaveTransformTests
    {
        [Test]
        public void Codecs_SelectPlainForTinyAndGzipForCompressibleLargePayloads()
        {
            var selector = new BeneficialCompressionCodec(16);
            Assert.That(selector.Encode(Encoding.UTF8.GetBytes("{}")).EncodingId, Is.EqualTo("json"));

            byte[] large = Encoding.UTF8.GetBytes(new string('x', 1000));
            var encoded = selector.Encode(large);
            Assert.That(encoded.EncodingId, Is.EqualTo("gzip-base64"));
            CollectionAssert.AreEqual(large, selector.Decode(encoded));
        }

        [Test]
        public void Protectors_DetectPayloadMetadataAndKeyChanges()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("payload");
            var sha = new Sha256SaveProtector();
            string checksum = sha.Protect(bytes);
            Assert.That(sha.Verify(bytes, checksum), Is.True);
            Assert.That(sha.Verify(Encoding.UTF8.GetBytes("changed"), checksum), Is.False);

            var hmac = new HmacSha256SaveProtector(Enumerable.Repeat((byte)7, 32).ToArray());
            var other = new HmacSha256SaveProtector(Enumerable.Repeat((byte)8, 32).ToArray());
            string tag = hmac.Protect(bytes);
            Assert.That(hmac.Verify(bytes, tag), Is.True);
            Assert.That(other.Verify(bytes, tag), Is.False);
        }

        [Test]
        public void LocalSecretKeyProvider_PersistsThirtyTwoByteKey()
        {
            string root = Path.Combine(Path.GetTempPath(), "comhung-key-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var provider = new LocalSecretKeyProvider(root);
                byte[] first = provider.GetOrCreateKey("persistence-hmac-v1").Key;
                byte[] second = provider.GetOrCreateKey("persistence-hmac-v1").Key;

                Assert.That(first.Length, Is.EqualTo(32));
                CollectionAssert.AreEqual(first, second);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }
    }
}
