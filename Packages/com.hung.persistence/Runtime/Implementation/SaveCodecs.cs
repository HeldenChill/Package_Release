using System;
using System.IO;
using System.IO.Compression;
using Hung.Base.Persistence;

namespace Hung.Data.Persistence
{
    public sealed class PlainJsonSaveCodec : ISaveCodec
    {
        public string EncodingId => "json";

        public SaveEncodedPayload Encode(byte[] jsonBytes) => new SaveEncodedPayload(EncodingId, jsonBytes);

        public byte[] Decode(SaveEncodedPayload payload) => payload.Bytes;
    }

    public sealed class GZipSaveCodec : ISaveCodec
    {
        public string EncodingId => "gzip-base64";

        public SaveEncodedPayload Encode(byte[] jsonBytes)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true))
            {
                gzip.Write(jsonBytes, 0, jsonBytes.Length);
            }
            return new SaveEncodedPayload(EncodingId, output.ToArray());
        }

        public byte[] Decode(SaveEncodedPayload payload)
        {
            using var input = new MemoryStream(payload.Bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }

    public sealed class BeneficialCompressionCodec : ISaveCodec
    {
        private readonly PlainJsonSaveCodec plain = new PlainJsonSaveCodec();
        private readonly GZipSaveCodec gzip = new GZipSaveCodec();
        private readonly int thresholdBytes;

        public BeneficialCompressionCodec(int thresholdBytes = 16 * 1024)
        {
            this.thresholdBytes = thresholdBytes;
        }

        public string EncodingId => "json-or-gzip";

        public SaveEncodedPayload Encode(byte[] jsonBytes)
        {
            if (jsonBytes == null)
                jsonBytes = Array.Empty<byte>();
            if (jsonBytes.Length < thresholdBytes)
                return plain.Encode(jsonBytes);

            SaveEncodedPayload compressed = gzip.Encode(jsonBytes);
            return compressed.Bytes.Length < jsonBytes.Length ? compressed : plain.Encode(jsonBytes);
        }

        public byte[] Decode(SaveEncodedPayload payload)
        {
            if (payload.EncodingId == plain.EncodingId)
                return plain.Decode(payload);
            if (payload.EncodingId == gzip.EncodingId)
                return gzip.Decode(payload);
            throw new InvalidOperationException($"Unknown payload encoding '{payload.EncodingId}'.");
        }
    }
}
