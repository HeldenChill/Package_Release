using System;
using System.Linq;
using Hung.Base;
using Hung.Base.Editor;
using NUnit.Framework;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Hung.Base.Editor.Tests
{
    public sealed class IdHolder : ScriptableObject
    {
        public AudioId Sound;
        public ItemId Item;
    }

    public sealed class AudioIdDrawerTests
    {
        [Test]
        public void Drawer_IsTheSingleGlobalAudioIdDrawerOwner()
        {
            Type drawerType = typeof(AudioIdDrawer);
            Assert.That(drawerType.BaseType, Is.EqualTo(typeof(OdinValueDrawer<AudioId>)));
            Assert.That(drawerType.Assembly.GetName().Name, Is.EqualTo("Hung.Base.Editor"));

            var allDrawers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsClass && !t.IsAbstract && typeof(OdinValueDrawer<AudioId>).IsAssignableFrom(t))
                .ToArray();

            Assert.That(allDrawers.Length, Is.EqualTo(1));
            Assert.That(allDrawers[0], Is.EqualTo(typeof(AudioIdDrawer)));
        }

        [Test]
        public void Odin_ResolvesBothIdDrawers()
        {
            var holder = ScriptableObject.CreateInstance<IdHolder>();
            PropertyTree tree = null;
            try
            {
                tree = PropertyTree.Create(holder);
                var soundProp = tree.GetPropertyAtPath("Sound");
                var itemProp = tree.GetPropertyAtPath("Item");

                var soundDrawers = soundProp.GetActiveDrawerChain().BakedDrawerArray;
                var itemDrawers = itemProp.GetActiveDrawerChain().BakedDrawerArray;

                Assert.That(soundDrawers.Any(d => d is AudioIdDrawer), Is.True, "AudioIdDrawer not found in Sound property drawer chain.");
                Assert.That(itemDrawers.Any(d => d is ItemIdDrawer), Is.True, "ItemIdDrawer not found in Item property drawer chain.");
            }
            finally
            {
                tree?.Dispose();
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }
    }
}
