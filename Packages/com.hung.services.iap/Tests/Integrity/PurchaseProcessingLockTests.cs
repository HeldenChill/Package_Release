using System.Threading.Tasks;
using NUnit.Framework;
using Hung.Base;

namespace Hung.IAP.Tests
{
    public sealed class PurchaseProcessingLockTests
    {
        [Test]
        public void RunOrJoin_SyncCompletedFactory_RunsAgainOnNextCall()
        {
            var processingLock = new PurchaseProcessingLock();
            int calls = 0;
            var retryable = new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, "first");
            var completed = new PurchaseRequestResult(PurchaseRequestStatus.Completed, "second");

            PurchaseRequestResult first = processingLock.RunOrJoin("tx-1", () => { calls++; return Task.FromResult(retryable); }).Result;
            PurchaseRequestResult second = processingLock.RunOrJoin("tx-1", () => { calls++; return Task.FromResult(completed); }).Result;

            Assert.That(first.Status, Is.EqualTo(PurchaseRequestStatus.RetryableFailure));
            Assert.That(second.Status, Is.EqualTo(PurchaseRequestStatus.Completed));
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void RunOrJoin_PendingFactory_JoinsThenRunsAgainAfterCompletion()
        {
            var processingLock = new PurchaseProcessingLock();
            int calls = 0;
            var pending = new TaskCompletionSource<PurchaseRequestResult>();

            Task<PurchaseRequestResult> a = processingLock.RunOrJoin("tx-1", () => { calls++; return pending.Task; });
            Task<PurchaseRequestResult> b = processingLock.RunOrJoin("tx-1", () => { calls++; return pending.Task; });
            Assert.That(b, Is.SameAs(a));
            Assert.That(calls, Is.EqualTo(1));

            pending.SetResult(new PurchaseRequestResult(PurchaseRequestStatus.Completed, "done"));
            a.Wait();
            processingLock.RunOrJoin("tx-1", () => { calls++; return Task.FromResult(new PurchaseRequestResult(PurchaseRequestStatus.Completed, "again")); }).Wait();
            Assert.That(calls, Is.EqualTo(2));
        }
    }
}
