using System;
using Hung.Base;
using Hung.Base.Composition;
namespace Hung.Data.Rewards
{
    public sealed class RewardFeatureBinding<TContract, TSession> where TContract : class where TSession : class, TContract, IDisposable
    {
        private readonly Func<TSession> create;
        private readonly Func<TSession, FeatureStartResult> initialize;
        private TSession current;
        public RewardFeatureBinding(Func<TSession> create, Func<TSession, FeatureStartResult> initialize)
        { this.create = create ?? throw new ArgumentNullException(nameof(create)); this.initialize = initialize ?? throw new ArgumentNullException(nameof(initialize)); }
        public TSession Current => current;
        public FeatureRegistration Registration(string id, bool required, string[] dependencies)
            => new FeatureRegistration(id, required, dependencies, Start, Stop);
        private FeatureStartResult Start()
        {
            // Refuse all concurrently active owners before constructing/loading the candidate.
            if (ServiceSlots.TryGet<TContract>(out _)) return new FeatureStartResult(false, "REWARD_OWNER_ACTIVE");
            bool ready = false;
            try
            {
                current = create();
                if (current == null) return new FeatureStartResult(false, "REWARD_FACTORY_NULL");
                var result = initialize(current); if (!result.Success) return result;
                var registered = ServiceSlots.Register<TContract>(current);
                ready = registered == ServiceRegistration.Registered || registered == ServiceRegistration.AlreadyRegistered;
                return new FeatureStartResult(ready, ready ? null : "REWARD_REGISTRATION_REFUSED");
            }
            finally { if (!ready) Stop(); }
        }
        public void Stop()
        { var old = current; current = null; if (old == null) return; ServiceSlots.Unregister<TContract>(old); old.Dispose(); }
    }
}
