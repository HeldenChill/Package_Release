using System;
using System.Collections.Generic;
using Hung.Base;

namespace Hung.Ads
{
    public sealed class AdsRequestController
    {
        private readonly Dictionary<AdsRequestKind, AdsRequestContext> activeByKind = new Dictionary<AdsRequestKind, AdsRequestContext>();

        public bool TryBegin(AdsShowRequest request, Action<AdsShowResult> onCompleted, out AdsRequestContext context, out AdsShowResult rejection)
        {
            if (activeByKind.ContainsKey(request.Kind))
            {
                context = null;
                rejection = new AdsShowResult(request.RequestId, AdsRequestOutcome.AlreadyRunning, false, "already-running");
                return false;
            }

            context = new AdsRequestContext(request, onCompleted, OnTerminal);
            activeByKind.Add(request.Kind, context);
            rejection = default;
            return true;
        }

        private void OnTerminal(AdsRequestContext context)
        {
            activeByKind.Remove(context.Request.Kind);
        }
    }
}
