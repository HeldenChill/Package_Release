using System;
using System.Collections.Generic;

namespace Hung.Ads
{
    public sealed class AdsLoadQueue
    {
        private readonly Queue<Action> pending = new Queue<Action>();
        private readonly Func<bool> canStart;
        private bool running;

        public AdsLoadQueue(Func<bool> canStart = null)
        {
            this.canStart = canStart ?? (() => true);
        }

        public int Count => pending.Count + (running ? 1 : 0);

        public void Enqueue(Action load)
        {
            if (load == null) throw new ArgumentNullException(nameof(load));
            pending.Enqueue(load);
            TryStartNext();
        }

        public void MarkCurrentComplete()
        {
            if (!running) return;
            running = false;
            TryStartNext();
        }

        public void Clear()
        {
            pending.Clear();
            running = false;
        }

        public void TryStartNext()
        {
            if (running || pending.Count == 0 || !canStart()) return;
            running = true;
            pending.Dequeue().Invoke();
        }
    }
}
