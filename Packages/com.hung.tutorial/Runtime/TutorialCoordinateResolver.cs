using System;
using Hung.Base;
using UnityEngine;

namespace Hung.Tutorial
{
    public class TutorialCoordinateResolver
    {
        private readonly Func<Vector3, RectTransform, Vector3> _worldToUI;

        public TutorialCoordinateResolver(Func<Vector3, RectTransform, Vector3> worldToUI)
        {
            _worldToUI = worldToUI;
        }

        public void ResolveHand(TutorialStepContext ctx, RectTransform canvasRect,
            Vector3 pos1, Vector3 pos2, POSITION_TYPE posType)
        {
            switch (posType)
            {
                case POSITION_TYPE.WORLD_POS:
                    ctx.Pos1 = _worldToUI(pos1, canvasRect);
                    ctx.Pos2 = _worldToUI(pos2, canvasRect);
                    break;
                case POSITION_TYPE.UI_POS:
                    ctx.Pos1 = pos1;
                    ctx.Pos2 = pos2;
                    break;
            }
        }
    }
}
