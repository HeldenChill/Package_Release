using Hung.DesignPattern;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Utilities
{
    using Hung.DesignPattern;
    public class MainThreadDispatcher : SimpleSingleton<MainThreadDispatcher>
    {
        private static readonly Queue<Action> executionQueue = new Queue<Action>();

        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        void Update()
        {
            // Snapshot under the lock, run outside it: work enqueued by an action runs next frame
            // instead of looping forever, and one throwing action does not drop the rest (BUG-261114-6a03).
            Action[] batch;
            lock (executionQueue)
            {
                if (executionQueue.Count == 0) return;
                batch = executionQueue.ToArray();
                executionQueue.Clear();
            }
            foreach (Action action in batch)
            {
                try { action?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        public void Enqueue(Action action)
        {
            lock (executionQueue)
            {
                executionQueue.Enqueue(action);
            }
        }
    }
}