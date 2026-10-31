using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    public enum ItemPresentationStatus { Unavailable, Found, UnknownItem, InvalidItem }
    public interface IItemPresentationProvider
    {
        ItemPresentationStatus GetPresentation(ItemId id, out ItemPresentation presentation);
    }
    public enum RewardPresentationOutcome { Unavailable, Presented }
    public sealed class RewardPresentationRequest
    {
        public RewardPresentationRequest(IReadOnlyList<RewardGrantItem> items, Vector3? origin = null)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("Empty reward.", nameof(items));
            var copy = new RewardGrantItem[items.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (!items[i].ItemId.IsValid || items[i].Quantity <= 0) throw new ArgumentException("Invalid reward.");
                copy[i] = items[i];
            }
            Items = Array.AsReadOnly(copy); Origin = origin;
        }
        public IReadOnlyList<RewardGrantItem> Items { get; }
        public Vector3? Origin { get; }
    }
    public interface IRewardPresenter
    {
        RewardPresentationOutcome Present(RewardPresentationRequest request);
    }
}
