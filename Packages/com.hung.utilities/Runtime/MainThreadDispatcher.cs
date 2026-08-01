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
            lock (executionQueue)
            {
                while (executionQueue.Count > 0)
                {
                    var action = executionQueue.Dequeue();
                    action.Invoke();
                }
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