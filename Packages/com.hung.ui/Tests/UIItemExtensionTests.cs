using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.UI.Tests
{
    public sealed class IconHookProbeItem : UIItem
    {
        public int Calls;
        public Sprite LastSprite;

        public void Configure(Image image) => icon = image;

        protected override void OnIconSpriteAssigned(Sprite sprite)
        {
            Calls++;
            LastSprite = sprite;
        }
    }

    public sealed class UIItemExtensionTests
    {
        [Test]
        public void Assigning_icon_invokes_product_extension_hook_once()
        {
            var gameObject = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(IconHookProbeItem));
            try
            {
                IconHookProbeItem item = gameObject.GetComponent<IconHookProbeItem>();
                item.Configure(gameObject.GetComponent<Image>());
                item.SetData((Sprite)null);

                Assert.AreEqual(1, item.Calls);
                Assert.IsNull(item.LastSprite);
            }
            finally { Object.DestroyImmediate(gameObject); }
        }
    }
}
