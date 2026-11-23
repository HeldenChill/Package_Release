using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hung.Base;

namespace Hung.IAP
{
    public sealed class PurchaseProcessingLock
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, Task<PurchaseRequestResult>> tasksByTransaction = new Dictionary<string, Task<PurchaseRequestResult>>(StringComparer.Ordinal);

        public Task<PurchaseRequestResult> RunOrJoin(string transactionId, Func<Task<PurchaseRequestResult>> factory)
        {
            lock (gate)
            {
                if (tasksByTransaction.TryGetValue(transactionId, out Task<PurchaseRequestResult> existing))
                    return existing;

                Task<PurchaseRequestResult> task = RunAndRemove(transactionId, factory);
                // A factory that finished synchronously already ran its remove, before this add.
                // Adding it now would cache the finished result for every later call of this transaction.
                if (!task.IsCompleted)
                    tasksByTransaction.Add(transactionId, task);
                return task;
            }
        }

        private async Task<PurchaseRequestResult> RunAndRemove(string transactionId, Func<Task<PurchaseRequestResult>> factory)
        {
            try
            {
                return await factory().ConfigureAwait(false);
            }
            finally
            {
                lock (gate)
                    tasksByTransaction.Remove(transactionId);
            }
        }
    }
}
