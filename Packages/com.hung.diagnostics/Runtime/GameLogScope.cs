using System;

namespace Hung.Diagnostics
{
    public readonly struct GameLogScope : IDisposable
    {
        private readonly string previousFlowId;
        private readonly string flowId;
        private readonly bool shouldEndFlow;

        public GameLogScope(string previousFlowId, string flowId, bool shouldEndFlow)
        {
            this.previousFlowId = previousFlowId;
            this.flowId = flowId;
            this.shouldEndFlow = shouldEndFlow;
        }

        public void Dispose()
        {
            if (shouldEndFlow)
            {
                GameLog.EndFlow(flowId);
            }

            GameLog.SetCurrentFlow(previousFlowId);
        }
    }
}