using System;
using System.Collections.Generic;

namespace Hung.Analytics.Tracking
{
    /// <summary>One bucket: values up to and including <see cref="upTo"/> get <see cref="label"/>.</summary>
    [Serializable]
    public sealed class Bucket
    {
        /// <summary>Inclusive upper bound.</summary>
        public double upTo;
        /// <summary>Emitted label.</summary>
        public string label;
        /// <summary>For serialization.</summary>
        public Bucket() { }
        /// <summary>Creates a bucket.</summary>
        public Bucket(double upTo, string label) { this.upTo = upTo; this.label = label; }
    }

    /// <summary>Ordered buckets; see <see cref="Label"/>.</summary>
    [Serializable]
    public sealed class BucketSet
    {
        /// <summary>Ascending by <see cref="Bucket.upTo"/>.</summary>
        public List<Bucket> buckets = new List<Bucket>();
        /// <summary>For serialization.</summary>
        public BucketSet() { }
        /// <summary>Creates a set from ascending buckets.</summary>
        public BucketSet(params Bucket[] items) { buckets.AddRange(items); }

        /// <summary>The first bucket whose bound is at or above <paramref name="value"/>; the last bucket catches anything larger; "" when empty.</summary>
        public string Label(double value)
        {
            if (buckets == null || buckets.Count == 0) return "";
            foreach (var b in buckets)
                if (b != null && value <= b.upTo) return b.label;
            return buckets[buckets.Count - 1]?.label ?? "";
        }
    }
}
