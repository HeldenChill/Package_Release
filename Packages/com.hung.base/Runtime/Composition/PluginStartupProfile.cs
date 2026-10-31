using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Hung.Base.Composition
{
    /// <summary>Product-owned optional plugin suppression; packages ship no instance.</summary>
    [CreateAssetMenu(menuName = "Hung/Composition/Plugin startup profile")]
    public sealed class PluginStartupProfile : ScriptableObject
    {
        [SerializeField] private List<string> disabledFeatureIds = new List<string>();
        /// <summary>Read-only configured disabled IDs; absence means no suppression.</summary>
        public IReadOnlyList<string> DisabledFeatureIds => disabledFeatureIds == null
            ? (IReadOnlyList<string>)Array.Empty<string>() : disabledFeatureIds.AsReadOnly();
        /// <summary>Checks the stable lowercase plugin-ID grammar.</summary>
        public static bool IsValidFeatureId(string id) => id != null &&
            Regex.IsMatch(id, @"\A[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*\z", RegexOptions.CultureInvariant);
        /// <summary>Returns every malformed/duplicate ID; never silently trims stored identities.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < DisabledFeatureIds.Count; index++)
            {
                string id = DisabledFeatureIds[index];
                if (!IsValidFeatureId(id)) errors.Add("Invalid disabled feature ID at index " + index + ".");
                else if (!seen.Add(id)) errors.Add("Duplicate disabled feature ID: " + id + ".");
            }
            return errors.AsReadOnly();
        }
    }
}
