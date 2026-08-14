using System;
using System.Collections.Generic;

namespace Hung.Base
{
    public sealed class PauseService : IPauseService
    {
        private readonly Dictionary<PauseLeaseId, PauseLease> leases = new Dictionary<PauseLeaseId, PauseLease>();
        private readonly ITimeScale timeScale;
        private readonly float runningScale;

        public PauseService(ITimeScale timeScale, float runningScale = 1f)
        {
            this.timeScale = timeScale ?? throw new ArgumentNullException(nameof(timeScale));
            this.runningScale = runningScale;
        }

        public bool IsPaused => leases.Count != 0;
        public int ActiveLeaseCount => leases.Count;

        public bool Acquire(PauseLease lease)
        {
            if (leases.ContainsKey(lease.Id)) return false;
            leases.Add(lease.Id, lease);
            timeScale.Scale = 0f;
            return true;
        }

        public bool Release(PauseLeaseId id)
        {
            if (!leases.Remove(id)) return false;
            if (leases.Count == 0) timeScale.Scale = runningScale;
            return true;
        }

        public void ReleaseOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) return;
            var ids = new List<PauseLeaseId>();
            foreach (var pair in leases)
            {
                if (string.Equals(pair.Value.Owner, owner, StringComparison.Ordinal)) ids.Add(pair.Key);
            }
            foreach (var id in ids) Release(id);
        }
    }
}
