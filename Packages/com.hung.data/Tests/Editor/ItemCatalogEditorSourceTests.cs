using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hung.Base;
using Hung.Base.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Editor.Tests
{
    public sealed class ItemCatalogEditorSourceTests
    {
        [Test]
        public void CatalogProvider_IsOptionalAndMergesWithBaseDefinitions()
        {
            ItemCatalog catalog = Catalog(Definition("game.gem"));
            var registry = new ItemIdEditorRegistry(new IItemIdEditorSource[]
            {
                new ItemCatalogEditorSource(() => new[] { catalog })
            });

            string[] ids = registry.GetOptions().Select(option => option.Id.Value).ToArray();

            Assert.That(ids, Does.Contain("base.gold"));
            Assert.That(ids, Does.Contain("game.gem"));
        }

        [Test]
        public void CatalogProvider_EmitsValidRowsWithNamespaceGroup()
        {
            ItemCatalog catalog = Catalog(Definition("game.gem"));
            var source = new ItemCatalogEditorSource(() => new[] { catalog });

            ItemIdEditorOption option = source.GetOptions().Single();

            Assert.That(option.Id.Value, Is.EqualTo("game.gem"));
            Assert.That(option.GroupPath, Is.EqualTo("game"));
        }

        private static ItemCatalog Catalog(params ItemDefinition[] definitions)
        {
            ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            SetField(catalog, "definitions", new List<ItemDefinition>(definitions));
            return catalog;
        }

        private static ItemDefinition Definition(string id)
        {
            var definition = new ItemDefinition();
            SetField(definition, "id", ItemId.Parse(id));
            return definition;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
