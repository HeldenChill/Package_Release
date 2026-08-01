using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hung.Base;
using Hung.Data;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests
{
    public class ItemCatalogTests
    {
        [Test]
        public void RebuildIndex_DuplicateId_Throws()
        {
            ItemCatalog catalog = Catalog(
                Definition("base.gold", "Gold"),
                Definition("base.gold", "Gold2"));

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => catalog.RebuildIndex());

            StringAssert.Contains("base.gold", exception.Message);
            StringAssert.Contains("1", exception.Message);
        }

        [Test]
        public void RebuildIndex_DuplicateCodeName_Throws()
        {
            ItemCatalog catalog = Catalog(
                Definition("base.gold", "Gold"),
                Definition("base.heart", "Gold"));

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => catalog.RebuildIndex());

            StringAssert.Contains("Gold", exception.Message);
            StringAssert.Contains("1", exception.Message);
        }

        [Test]
        public void GetRequired_KnownId_ReturnsDefinition()
        {
            ItemCatalog catalog = Catalog(Definition("base.gold", "Gold"));
            catalog.RebuildIndex();

            Assert.AreEqual("Gold", catalog.GetRequired(BaseItemIds.Gold).CodeName);
        }

        [Test]
        public void GetRequired_UnknownId_Throws()
        {
            ItemCatalog catalog = Catalog(Definition("base.gold", "Gold"));
            catalog.RebuildIndex();

            KeyNotFoundException exception =
                Assert.Throws<KeyNotFoundException>(() => catalog.GetRequired(BaseItemIds.Heart));

            StringAssert.Contains("base.heart", exception.Message);
        }

        [Test]
        public void Ids_AreSortedDeterministically()
        {
            ItemCatalog catalog = Catalog(
                Definition("base.heart", "Heart"),
                Definition("base.gold", "Gold"),
                Definition("pet_vs_monster.gem", "Gem"));
            catalog.RebuildIndex();

            CollectionAssert.AreEqual(
                new[] { "base.gold", "base.heart", "pet_vs_monster.gem" },
                catalog.Ids.Select(id => id.Value).ToArray());
        }

        [Test]
        public void RebuildIndex_NullDefinition_Throws()
        {
            ItemCatalog catalog = Catalog((ItemDefinition)null);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => catalog.RebuildIndex());

            StringAssert.Contains("0", exception.Message);
        }

        [Test]
        public void DataManager_GetSOData_ReturnsComposedItemCatalog()
        {
            var gameObject = new GameObject("DataManagerFixture");
            try
            {
                DataManager dataManager = gameObject.AddComponent<DataManager>();
                ItemCatalog catalog = Catalog(Definition("base.gold", "Gold"));
                SetField(dataManager, "itemCatalog", catalog);

                Assert.AreSame(catalog, dataManager.GetSOData<ItemCatalog>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static ItemCatalog Catalog(params ItemDefinition[] definitions)
        {
            ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            SetField(catalog, "definitions", new List<ItemDefinition>(definitions));
            return catalog;
        }

        private static ItemDefinition Definition(string id, string codeName)
        {
            var definition = new ItemDefinition();
            SetField(definition, "id", ItemId.Parse(id));
            SetField(definition, "codeName", codeName);
            return definition;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }
    }
}
