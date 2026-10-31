using System;
using System.Collections.Generic;

namespace Hung.Base.Persistence
{
    /// <summary>Validates the ownership of model types, save names and canonical filenames in one persistence graph without performing I/O.</summary>
    public static class SaveDefinitionSet
    {
        /// <summary>Enumerates definitions once and rejects conflicting owners. An empty set and a definition's own canonical-key alias are valid.</summary>
        /// <exception cref="ArgumentNullException">The input sequence is null.</exception>
        /// <exception cref="ArgumentException">An entry is null or definitions have conflicting model, name or canonical filename ownership.</exception>
        public static void Validate(IEnumerable<SaveDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var models = new HashSet<Type>();
            var names = new Dictionary<string, SaveDefinition>(StringComparer.Ordinal);
            var files = new Dictionary<string, SaveDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (SaveDefinition definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Save definition cannot be null.", nameof(definitions));
                if (!models.Add(definition.ModelType))
                    throw new ArgumentException("SAVE_DEFINITION_MODEL_COLLISION: " + definition.ModelType.FullName + " key=" + definition.Key, nameof(definitions));
                AddName(names, definition.Key, definition);
                foreach (string alias in definition.LegacyPlayerPrefsKeys) AddName(names, alias, definition);
                if (files.TryGetValue(definition.Key, out SaveDefinition owner))
                    throw new ArgumentException("SAVE_DEFINITION_FILE_COLLISION: " + owner.Key + " / " + definition.Key, nameof(definitions));
                files.Add(definition.Key, definition);
            }
        }

        private static void AddName(Dictionary<string, SaveDefinition> names, string name, SaveDefinition definition)
        {
            if (names.TryGetValue(name, out SaveDefinition owner) && !ReferenceEquals(owner, definition))
                throw new ArgumentException("SAVE_DEFINITION_NAME_COLLISION: " + name + " owners=" + owner.ModelType.FullName + " / " + definition.ModelType.FullName, nameof(definition));
            names[name] = definition;
        }
    }
}
