using System;
using UnityEngine;

namespace Hung.DesignPattern.ScopedPooling
{
    /// <summary>One product-authored mapping; identity is independent of display text.</summary>
    [Serializable]
    public sealed class PoolCatalogEntry
    {
        /// <summary>Product-defined stable exact identity.</summary>
        public string Identity;
        /// <summary>Existing prefab reference, no shared enum.</summary>
        public Component Prefab;
        /// <summary>Separate unit and particle namespaces.</summary>
        public PoolKind Kind;
        /// <summary>Preload count.</summary>
        public int Warmup;
        /// <summary>Distinct capacity; 0 means uncapped.</summary>
        public int HardLimit;
    }

    /// <summary>Optional standard Unity authoring; groups copy definitions when registered.</summary>
    [CreateAssetMenu(menuName = "Hung/Scoped Pool Catalog")]
    public sealed class PoolCatalogAsset : ScriptableObject, IPoolCatalogResolver
    {
        [SerializeField] private PoolCatalogEntry[] entries = Array.Empty<PoolCatalogEntry>();

        /// <summary>Replace authoring entries; this does not replace any registered scope snapshot.</summary>
        public void Configure(params PoolCatalogEntry[] values) => entries = values == null ? null : (PoolCatalogEntry[])values.Clone();

        /// <summary>Copy authoring values; group registration validates every entry and reports its index.</summary>
        public PoolStatus Resolve(out PoolDefinition[] definitions)
        {
            definitions = null;
            if (entries == null || entries.Length == 0) return PoolStatus.InvalidConfiguration;
            var result = new PoolDefinition[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                result[i] = e == null ? null : new PoolDefinition(e.Prefab, e.Kind,
                    e.Identity ?? string.Empty, e.Warmup, e.HardLimit);
            }
            definitions = result;
            return PoolStatus.Success;
        }
    }
}
