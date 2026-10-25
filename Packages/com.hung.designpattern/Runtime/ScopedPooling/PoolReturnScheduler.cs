using System;
using System.Collections.Generic;

namespace Hung.DesignPattern.ScopedPooling
{
    internal sealed class PoolReturnScheduler
    {
        private sealed class Pending
        {
            internal long Id;
            internal PoolCheckout Checkout;
            internal double Remaining;
            internal PoolClock Clock;
        }

        private readonly PoolScope scope;
        private readonly Dictionary<long, Pending> pending = new Dictionary<long, Pending>();
        private long nextId;

        internal PoolReturnScheduler(PoolScope scope) { this.scope = scope; }

        internal PoolStatus Schedule(PoolCheckout checkout, float delay, PoolClock clock, out ReturnRequest request)
        {
            request = default;
            if (float.IsNaN(delay) || float.IsInfinity(delay) || delay < 0) return PoolStatus.InvalidDelay;
            if (clock != PoolClock.Scaled && clock != PoolClock.Unscaled) return PoolStatus.InvalidConfiguration;
            if (!ReferenceEquals(checkout.Scope, scope)) return PoolStatus.WrongScope;
            if (!checkout.IsValid) return PoolStatus.StaleCheckout;
            Cancel(checkout);
            var item = new Pending { Id = ++nextId, Checkout = checkout, Remaining = delay, Clock = clock };
            pending.Add(item.Id, item);
            request = new ReturnRequest(scope, item.Id);
            return PoolStatus.Success;
        }

        internal bool Contains(ReturnRequest request) => ReferenceEquals(request.Scope, scope) &&
            pending.TryGetValue(request.Id, out var item) && item.Checkout.IsValid;

        internal PoolStatus Cancel(ReturnRequest request)
        {
            if (!ReferenceEquals(request.Scope, scope)) return PoolStatus.WrongScope;
            return pending.Remove(request.Id) ? PoolStatus.Success : PoolStatus.StaleRequest;
        }

        internal void Cancel(PoolCheckout checkout)
        {
            foreach (var item in new List<Pending>(pending.Values))
                if (item.Checkout.Slot == checkout.Slot && item.Checkout.Generation == checkout.Generation)
                    pending.Remove(item.Id);
        }

        internal void Clear() => pending.Clear();

        internal void Tick(float scaledDelta, float unscaledDelta)
        {
            foreach (var item in new List<Pending>(pending.Values))
            {
                if (!pending.ContainsKey(item.Id)) continue;
                if (!item.Checkout.IsValid)
                {
                    pending.Remove(item.Id);
                    continue;
                }
                item.Remaining -= item.Clock == PoolClock.Scaled ? scaledDelta : unscaledDelta;
                if (item.Remaining > 0) continue;
                pending.Remove(item.Id);
                if (item.Checkout.IsValid) scope.Return(item.Checkout);
            }
        }
    }

    public sealed partial class PoolScope
    {
        private PoolReturnScheduler scheduler;
        private PoolReturnScheduler Scheduler => scheduler ?? (scheduler = new PoolReturnScheduler(this));

        /// <summary>Schedule or replace one checkout's delayed return; zero is deferred until a later tick.</summary>
        public PoolStatus ScheduleReturn(PoolCheckout checkout, float delay, PoolClock clock, out ReturnRequest request)
        {
            request = default;
            return Live() == PoolStatus.Success ? Scheduler.Schedule(checkout, delay, clock, out request) : PoolStatus.DisposedScope;
        }

        /// <summary>Cancel exactly this request; an old handle cannot cancel its replacement.</summary>
        public PoolStatus Cancel(ReturnRequest request) => Live() == PoolStatus.Success ? Scheduler.Cancel(request) : PoolStatus.DisposedScope;

        /// <summary>Advance timers and particle completion once; runtime runner owns this call, tests may drive it.</summary>
        public void Tick(float scaledDelta, float unscaledDelta)
        {
            if (float.IsNaN(scaledDelta) || float.IsInfinity(scaledDelta) || scaledDelta < 0 ||
                float.IsNaN(unscaledDelta) || float.IsInfinity(unscaledDelta) || unscaledDelta < 0)
                throw new ArgumentOutOfRangeException(nameof(scaledDelta));
            if (Live() != PoolStatus.Success) return;
            scheduler?.Tick(scaledDelta, unscaledDelta);
            TickParticles();
        }

        partial void CancelForCheckout(PoolCheckout checkout) => scheduler?.Cancel(checkout);
        partial void CancelAllRequests() => scheduler?.Clear();
        partial void CheckPending(ReturnRequest request, ref bool pending)
        {
            pending = Live() == PoolStatus.Success && scheduler != null && scheduler.Contains(request);
        }
        partial void TickParticles();
    }
}
