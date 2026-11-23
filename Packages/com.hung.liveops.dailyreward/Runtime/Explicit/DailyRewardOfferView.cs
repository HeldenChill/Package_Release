using System;
using System.Linq;
using Hung.Base;
using Hung.Data.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Hung.LiveOps.DailyReward
{
    // Opt-in component. Existing prefab field IDs and legacy popup remain unchanged.
    public sealed class DailyRewardOfferView : MonoBehaviour
    {
        [SerializeField] private TMP_Text contents;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button claim;
        private DailyRewardSession session;
        private IItemPresentationProvider provider;
        private IRewardPresenter presenter;
        private Func<RewardAuthorization?> authorization;
        private int slot;
        private bool busy;
        public void Configure(DailyRewardSession session, IItemPresentationProvider provider,
            IRewardPresenter presenter, int slot = -1, Func<RewardAuthorization?> authorization = null)
        { this.session = session; this.provider = provider; this.presenter = presenter; this.slot = slot; this.authorization = authorization; Refresh(); }
        private void Awake() { if (claim != null) claim.onClick.AddListener(Claim); }
        private void OnDestroy() { if (claim != null) claim.onClick.RemoveListener(Claim); }
        private ResolvedRewardOffer Offer => session == null ? null : slot < 0 ? session.PreviewFree() : session.PreviewSequence(slot);
        public bool Refresh()
        {
            var offer = Offer;
            bool visible = offer != null && provider != null && contents != null;
            var labels = new System.Collections.Generic.List<string>();
            if (visible) foreach (var item in offer.Items)
            {
                if (provider.GetPresentation(item.ItemId, out var p) != ItemPresentationStatus.Found || string.IsNullOrWhiteSpace(p.DisplayName)) { visible = false; break; }
                labels.Add(p.DisplayName + " × " + item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            if (contents != null) contents.text = visible ? string.Join("\n", labels) : "Reward unavailable";
            if (claim != null) claim.interactable = visible && !busy;
            return visible;
        }
        public void Claim()
        {
            if (busy || !Refresh()) return;
            var offer = Offer; busy = true; Refresh();
            try
            {
                var result = slot < 0 ? session.ClaimFree() : session.ClaimSequence(slot, authorization?.Invoke());
                if (status != null) status.text = result.Success ? "Claimed" : result.DiagnosticCode ?? result.Outcome.ToString();
                if (result.Outcome == RewardGrantOutcome.Success && presenter != null)
                    presenter.Present(new RewardPresentationRequest(offer.Items, transform.position));
            }
            finally { busy = false; Refresh(); }
        }
    }
}
