using System;
using Hung.Base;
using Hung.Data.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Hung.LiveOps.SpinWheel
{
    public sealed class SpinRewardOfferView : MonoBehaviour
    {
        [SerializeField] private TMP_Text contents;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button spin;
        public event Action<string> Prepared;
        private SpinWheelSession session;
        private IItemPresentationProvider provider;
        private IRewardPresenter presenter;
        private Func<RewardAuthorization?> authorization;
        private int selectedIndex;
        private bool isAds, busy;
        private string pendingId;
        private ResolvedRewardOffer preparedOffer;
        public void Configure(SpinWheelSession session, IItemPresentationProvider provider, IRewardPresenter presenter,
            int selectedIndex, bool isAds, Func<RewardAuthorization?> authorization = null)
        { this.session = session; this.provider = provider; this.presenter = presenter; this.selectedIndex = selectedIndex; this.isAds = isAds; this.authorization = authorization; Refresh(); }
        private void Awake() { if (spin != null) spin.onClick.AddListener(Prepare); }
        private void OnDestroy() { if (spin != null) spin.onClick.RemoveListener(Prepare); }
        public bool Refresh()
        {
            var offer = session?.Preview(selectedIndex, isAds);
            bool visible = offer != null && provider != null && contents != null;
            var labels = new System.Collections.Generic.List<string>();
            if (visible) foreach (var item in offer.Items)
            {
                if (provider.GetPresentation(item.ItemId, out var p) != ItemPresentationStatus.Found || string.IsNullOrWhiteSpace(p.DisplayName)) { visible = false; break; }
                labels.Add(p.DisplayName + " × " + item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            if (contents != null) contents.text = visible ? string.Join("\n", labels) : "Reward unavailable";
            if (spin != null) spin.interactable = visible && !busy;
            return visible;
        }
        public void Prepare()
        {
            if (busy || !Refresh()) return;
            var auth = authorization?.Invoke();
            if (isAds && !auth.HasValue) { if (status != null) status.text = "AUTHORIZATION_REQUIRED"; return; }
            var result = isAds ? session.PrepareAuthorizedSpin(selectedIndex, auth.Value) : session.PrepareFreeSpin(selectedIndex);
            if (!result.Success) { if (status != null) status.text = result.DiagnosticCode; return; }
            pendingId = result.SpinId; preparedOffer = session.Preview(selectedIndex, isAds); busy = true; Refresh();
            if (status != null) status.text = "Pending";
            Prepared?.Invoke(pendingId); // Host may animate; completion is a separate acknowledged call.
        }
        public void Complete()
        {
            if (string.IsNullOrWhiteSpace(pendingId)) return;
            var result = session.CompleteSpin(pendingId);
            if (status != null) status.text = result.Success ? "Claimed" : result.DiagnosticCode ?? result.Outcome.ToString();
            if (result.Outcome == RewardGrantOutcome.Success && presenter != null)
                presenter.Present(new RewardPresentationRequest(preparedOffer.Items, transform.position));
            if (result.Success) { pendingId = null; preparedOffer = null; }
            busy = false; Refresh();
        }
    }
}
