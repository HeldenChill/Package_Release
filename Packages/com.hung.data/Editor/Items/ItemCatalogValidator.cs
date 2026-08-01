using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hung.Base;

namespace Hung.Data.Editor
{
    public static class ItemCatalogValidator
    {
        private static readonly Regex CodeNamePattern = new("^@?[A-Za-z_][A-Za-z0-9_]*$");

        public static void ValidateForCodeGeneration(IEnumerable<ItemDefinition> definitions)
        {
            var codeNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ItemDefinition definition in definitions)
            {
                if (definition == null)
                    throw new InvalidOperationException("Item definition is null.");

                ItemId id = definition.Id;
                if (!id.IsValid)
                    throw new InvalidOperationException($"Item id '{id}' is invalid.");

                if (id.Value.StartsWith("base.", StringComparison.Ordinal))
                    continue;

                string codeName = definition.CodeName;
                if (string.IsNullOrEmpty(codeName) || !CodeNamePattern.IsMatch(codeName))
                    throw new InvalidOperationException($"Item code name '{codeName}' is invalid.");

                if (!codeNames.Add(codeName))
                    throw new InvalidOperationException($"Item code name '{codeName}' is duplicated.");
            }
        }
    }
}
