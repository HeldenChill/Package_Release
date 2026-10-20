using System;
using System.Collections.Generic;

namespace Hung.Base
{
    /// <summary>
    /// Counted pause: the first lease freezes time, the last release resumes it. The resume speed is
    /// captured when the first lease is acquired, so a pause never changes the speed the game was
    /// running at (a boot-time constant restored over a 1.8x player speed was BUG-261019-b68c in PVM).
    /// The last release writes that speed only while the scale is still 0: a speed another owner wrote
    /// during the pause is kept.
    /// </summary>
    public sealed class PauseService : IPauseService
    {
        private readonly Dictionary<PauseLeaseId, PauseLease> leases = new Dictionary<PauseLeaseId, PauseLease>();
        private readonly ITimeScale timeScale;
        private float resumeScale;

        /// <param name="runningScale">Unused since 0.26.1; the resume speed is captured at the first
        /// <see cref="Acquire"/>. Kept so existing callers compile.</param>
        public PauseService(ITimeScale timeScale, float runningScale = 1f)
        {
            this.timeScale = timeScale ?? throw new ArgumentNullException(nameof(timeScale));
        }

        public bool IsPaused => leases.Count != 0;
        public int ActiveLeaseCount => leases.Count;

        public bool Acquire(PauseLease lease)
        {
            if (leases.ContainsKey(lease.Id)) return false;
            if (leases.Count == 0) resumeScale = timeScale.Scale;
            leases.Add(lease.Id, lease);
            timeScale.Scale = 0f;
            return true;
        }

        public bool Release(PauseLeaseId id)
        {
            if (!leases.Remove(id)) return false;
            if (leases.Count == 0 && timeScale.Scale == 0f) timeScale.Scale = resumeScale;
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

        public bool TryDeferResumeScale(float scale)
        {
            if (leases.Count == 0) return false;
            resumeScale = scale;
            return true;
        }
    }
}
