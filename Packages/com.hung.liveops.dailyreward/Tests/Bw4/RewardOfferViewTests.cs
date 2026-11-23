using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using Hung.Data.Rewards;
using NUnit.Framework;
using UnityEngine;
namespace Hung.LiveOps.DailyReward.Tests.Bw4
{
    public sealed class RewardOfferViewTests
    {
        private sealed class Presentation : IItemPresentationProvider, IRewardPresenter
        {
            internal bool Missing;
            internal int Effects;
            public ItemPresentationStatus GetPresentation(ItemId id, out ItemPresentation result)
            { result = new ItemPresentation(null, null, id.Value, default); return Missing ? ItemPresentationStatus.UnknownItem : ItemPresentationStatus.Found; }
            public RewardPresentationOutcome Present(RewardPresentationRequest request) { Effects++; Assert.That(request.Items[0].Quantity, Is.EqualTo(5)); return RewardPresentationOutcome.Presented; }
        }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static string Text(Component text) => (string)text.GetType().GetProperty("text").GetValue(text);
        [Test] public void MissingPresentationBlocksAndFailureNeverClaimsOrPresents()
        {
            using (var f = new Bw4Fixture())
            {
                var root = new GameObject("BW4 view fixture");
                try
                {
                    var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
                    var contentObject = new GameObject("contents", typeof(RectTransform)); contentObject.transform.SetParent(root.transform);
                    var statusObject = new GameObject("status", typeof(RectTransform)); statusObject.transform.SetParent(root.transform);
                    var content = contentObject.AddComponent(textType); var status = statusObject.AddComponent(textType);
                    
var session = new DailyRewardSession(f.DailyStore, f.Coordinator, f.Clock, new RewardDayPolicy(0), "profile-a", Bw4Fixture.Offer(), new[] { Bw4Fixture.Offer() }, new[] { false }, 3600);
Assert.That(session.Initialize(out _), Is.True);
var view = root.AddComponent<DailyRewardOfferView>(); Set(view, "contents", content); Set(view, "status", status);
var presentation = new Presentation { Missing = true }; view.Configure(session, presentation, presentation);
Assert.That(view.Refresh(), Is.False); view.Claim(); Assert.That(f.Grant.Published, Is.Zero);
presentation.Missing = false; f.Grant.Unavailable = true; view.Claim();
Assert.That(Text(status), Is.Not.EqualTo("Claimed")); Assert.That(presentation.Effects, Is.Zero);
Assert.That(Text(content), Does.Contain("5")); f.Grant.Unavailable = false; view.Claim();
Assert.That(Text(status), Is.EqualTo("Claimed")); Assert.That(presentation.Effects, Is.EqualTo(1));
view.Claim(); Assert.That(presentation.Effects, Is.EqualTo(1));

                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
    }
}
