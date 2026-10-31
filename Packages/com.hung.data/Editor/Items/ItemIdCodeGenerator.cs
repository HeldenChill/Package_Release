using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Hung.Base.Editor;

namespace Hung.Data.Editor
{
    public static class ItemIdCodeGenerator
    {
        public static string GenerateContent(IEnumerable<ItemDefinition> definitions, string className = "GameItemIds")
        {
            ItemDefinition[] source = definitions?.ToArray() ?? Array.Empty<ItemDefinition>();
            ItemCatalogValidator.ValidateForCodeGeneration(source);

            return StableIdCodeGenerator.GenerateContent(
                "ItemId",
                "Hung.Base",
                className,
                source.Select(definition => new KeyValuePair<string, string>(definition.CodeName, definition.Id.Value)),
                "base.");
        }

        public static bool WriteIfChanged(string path, string content)
        {
            return StableIdCodeGenerator.WriteIfChanged(path, content);
        }
    }
}
