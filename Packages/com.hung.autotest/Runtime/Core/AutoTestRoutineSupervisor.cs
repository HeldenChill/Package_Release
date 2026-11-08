using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Hung.AutoTest
{
    public enum SupervisorPhaseResult
    {
        None,
        Completed,
        Failed,
        TimedOut,
        Stopped
    }

    public sealed class AutoTestRoutineSupervisor : IEnumerator, IDisposable
    {
        private abstract class PendingWait
        {
            public abstract bool IsDone();
        }

        private sealed class CustomInstructionPendingWait : PendingWait
        {
            private readonly CustomYieldInstruction instruction;
            public CustomInstructionPendingWait(CustomYieldInstruction instruction) => this.instruction = instruction;
            public override bool IsDone() => instruction == null || !instruction.keepWaiting;
        }

        private sealed class AsyncOpPendingWait : PendingWait
        {
            private readonly AsyncOperation op;
            public AsyncOpPendingWait(AsyncOperation op) => this.op = op;
            public override bool IsDone() => op == null || op.isDone;
        }

        private sealed class ScaledSecondsPendingWait : PendingWait
        {
            private readonly float targetSeconds;
            private float elapsed;
            public ScaledSecondsPendingWait(float targetSeconds) => this.targetSeconds = targetSeconds;
            public override bool IsDone()
            {
                elapsed += Time.deltaTime;
                return elapsed >= targetSeconds;
            }
        }

        private static readonly FieldInfo WaitForSecondsField =
            typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Stack<IEnumerator> iteratorStack = new Stack<IEnumerator>();
        private readonly HashSet<IEnumerator> disposedIterators = new HashSet<IEnumerator>();
        private readonly List<AutoTestFailure> failures = new List<AutoTestFailure>();

        private readonly float deadlineRealtime;
        private readonly Func<bool> isCancelled;
        private readonly Func<float> realtimeClock;
        private readonly Action<AutoTestFailure> onFailure;
        private readonly AutoTestContext context;
        private readonly string phaseName;

        private PendingWait pendingWait;
        private object currentYield;

        public SupervisorPhaseResult PhaseResult { get; private set; } = SupervisorPhaseResult.None;
        public AutoTestFailure FirstFailure { get; private set; }
        public IReadOnlyList<AutoTestFailure> Failures => failures;
        public object Current => currentYield;

        public AutoTestRoutineSupervisor(
            IEnumerator rootRoutine,
            float deadlineRealtime = float.PositiveInfinity,
            Func<bool> isCancelled = null,
            Func<float> realtimeClock = null,
            Action<AutoTestFailure> onFailure = null,
            AutoTestContext context = null,
            string phaseName = "Execution")
        {
            this.deadlineRealtime = deadlineRealtime;
            this.isCancelled = isCancelled;
            this.realtimeClock = realtimeClock ?? (() => Time.realtimeSinceStartup);
            this.onFailure = onFailure;
            this.context = context;
            this.phaseName = string.IsNullOrEmpty(phaseName) ? "Execution" : phaseName;

            if (rootRoutine != null)
                iteratorStack.Push(rootRoutine);
        }

        public bool MoveNext()
        {
            if (PhaseResult != SupervisorPhaseResult.None)
                return false;

            if (isCancelled != null && isCancelled())
            {
                PhaseResult = SupervisorPhaseResult.Stopped;
                DisposeStack();
                return false;
            }

            float now = realtimeClock();
            if (now >= deadlineRealtime)
            {
                PhaseResult = SupervisorPhaseResult.TimedOut;
                RecordFailure(
                    "Timeout",
                    "AUTOTEST_DEADLINE_EXCEEDED",
                    $"Phase '{phaseName}' exceeded realtime deadline of {deadlineRealtime:F2}s (current realtime: {now:F2}s).",
                    null);
                DisposeStack();
                return false;
            }

            if (pendingWait != null)
            {
                if (!pendingWait.IsDone())
                {
                    currentYield = null;
                    return true;
                }

                pendingWait = null;
            }

            while (iteratorStack.Count > 0)
            {
                IEnumerator currentIter = iteratorStack.Peek();
                bool hasNext;
                try
                {
                    hasNext = currentIter.MoveNext();
                }
                catch (Exception ex)
                {
                    RecordFailure(phaseName, "AutoTestSupervisor", ex.Message, ex);
                    PhaseResult = SupervisorPhaseResult.Failed;
                    DisposeStack();
                    return false;
                }

                if (!hasNext)
                {
                    IEnumerator completed = iteratorStack.Pop();
                    DisposeEnumerator(completed);

                    if (iteratorStack.Count == 0)
                    {
                        if (PhaseResult == SupervisorPhaseResult.None)
                            PhaseResult = SupervisorPhaseResult.Completed;
                        return false;
                    }

                    continue;
                }

                object yielded = currentIter.Current;

                if (yielded == null)
                {
                    currentYield = null;
                    return true;
                }

                if (yielded is IEnumerator nested)
                {
                    iteratorStack.Push(nested);
                    continue;
                }

                if (yielded is CustomYieldInstruction custom)
                {
                    if (custom.keepWaiting)
                    {
                        pendingWait = new CustomInstructionPendingWait(custom);
                        currentYield = null;
                        return true;
                    }
                    continue;
                }

                if (yielded is AsyncOperation asyncOp)
                {
                    if (!asyncOp.isDone)
                    {
                        pendingWait = new AsyncOpPendingWait(asyncOp);
                        currentYield = null;
                        return true;
                    }
                    continue;
                }

                if (yielded is WaitForSeconds wfs)
                {
                    if (TryExtractWaitForSeconds(wfs, out float seconds))
                    {
                        if (seconds > 0f)
                        {
                            pendingWait = new ScaledSecondsPendingWait(seconds);
                            currentYield = null;
                            return true;
                        }
                        continue;
                    }

                    RecordFailure(
                        phaseName,
                        "AutoTestSupervisor",
                        "Cannot inspect duration of WaitForSeconds to supervise wait without suspending lifecycle tracking.",
                        null);
                    PhaseResult = SupervisorPhaseResult.Failed;
                    DisposeStack();
                    return false;
                }

                if (yielded is Coroutine)
                {
                    // Public APIs cannot poll a Coroutine handle, and forwarding it to Unity would suspend
                    // deadline and cancellation supervision. Fail explicitly instead.
                    RecordFailure(
                        phaseName,
                        "AutoTestSupervisor",
                        "Unsupported yield form: 'UnityEngine.Coroutine'. Yielding a Coroutine handle cannot be supervised (deadline and cancel would be suspended); yield the coroutine's IEnumerator instead.",
                        null);
                    PhaseResult = SupervisorPhaseResult.Failed;
                    DisposeStack();
                    return false;
                }

                RecordFailure(
                    phaseName,
                    "AutoTestSupervisor",
                    $"Unsupported yield form: '{yielded.GetType().FullName}'. Supported yields: null, nested IEnumerator, CustomYieldInstruction (incl. WaitForSecondsRealtime), AsyncOperation, WaitForSeconds.",
                    null);
                PhaseResult = SupervisorPhaseResult.Failed;
                DisposeStack();
                return false;
            }

            if (PhaseResult == SupervisorPhaseResult.None)
                PhaseResult = SupervisorPhaseResult.Completed;
            return false;
        }

        private static bool TryExtractWaitForSeconds(WaitForSeconds wfs, out float seconds)
        {
            if (WaitForSecondsField != null)
            {
                object val = WaitForSecondsField.GetValue(wfs);
                if (val is float f)
                {
                    seconds = f;
                    return true;
                }
            }

            seconds = 0f;
            return false;
        }

        private void RecordFailure(string category, string assertionId, string message, Exception ex)
        {
            if (FirstFailure == null)
            {
                FirstFailure = AutoTestFailure.Create(
                    category,
                    assertionId,
                    message,
                    AutoTestAssertionSeverity.Fatal,
                    context,
                    ex != null ? ex.ToString() : string.Empty);
                failures.Add(FirstFailure);
                onFailure?.Invoke(FirstFailure);
                context?.AddFailure(FirstFailure);
            }
            else
            {
                AutoTestFailure additional = AutoTestFailure.Create(
                    category,
                    assertionId,
                    message,
                    AutoTestAssertionSeverity.Failure,
                    context,
                    ex != null ? ex.ToString() : string.Empty);
                failures.Add(additional);
                onFailure?.Invoke(additional);
                context?.AddFailure(additional);
            }
        }

        private void RecordDisposalException(Exception ex)
        {
            if (FirstFailure == null)
            {
                FirstFailure = AutoTestFailure.Create(
                    "Disposal",
                    "AutoTestSupervisor",
                    ex.Message,
                    AutoTestAssertionSeverity.Fatal,
                    context,
                    ex.ToString());
                failures.Add(FirstFailure);
                onFailure?.Invoke(FirstFailure);
                context?.AddFailure(FirstFailure);
                PhaseResult = SupervisorPhaseResult.Failed;
            }
            else
            {
                AutoTestFailure additionalFailure = AutoTestFailure.Create(
                    "Disposal",
                    "AutoTestSupervisor",
                    "Disposal exception: " + ex.Message,
                    AutoTestAssertionSeverity.Failure,
                    context,
                    ex.ToString());
                failures.Add(additionalFailure);
                onFailure?.Invoke(additionalFailure);
                context?.AddFailure(additionalFailure);
            }
        }

        private void DisposeEnumerator(IEnumerator iter)
        {
            if (iter == null)
                return;

            if (!disposedIterators.Add(iter))
                return;

            if (iter is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    RecordDisposalException(ex);
                }
            }
        }

        private void DisposeStack()
        {
            while (iteratorStack.Count > 0)
            {
                IEnumerator iter = iteratorStack.Pop();
                DisposeEnumerator(iter);
            }
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
            DisposeStack();
        }
    }
}
