using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Hung.Base;
namespace Hung.Data.Rewards
{
    public static class RewardFingerprintV1
    {
        // BinaryWriter Int32: little endian. Strings: Int32 UTF-8 byte count + exact bytes.
        public static string Create(string profile, string feature, string occurrence, ResolvedRewardOffer offer)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    Text(writer, "bw4:v1"); Text(writer, profile); Text(writer, feature); Text(writer, occurrence);
                    Text(writer, offer.Key); Text(writer, offer.Revision); writer.Write(offer.Items.Count);
                    foreach (var item in offer.Items) { Text(writer, item.ItemId.Value); writer.Write(item.Quantity); }
                }
                using (var sha = SHA256.Create())
                {
                    byte[] bytes = sha.ComputeHash(stream.ToArray());
                    return "bw4:v1:" + BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        private static void Text(BinaryWriter writer, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Missing fingerprint context.");
            byte[] bytes = Encoding.UTF8.GetBytes(text); writer.Write(bytes.Length); writer.Write(bytes);
        }
    }
}
