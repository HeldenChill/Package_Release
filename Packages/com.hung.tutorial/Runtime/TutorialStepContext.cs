using UnityEngine;
using UnityEngine.UI;

namespace Hung.Tutorial
{
    public class TutorialStepContext
    {
        public Vector3 OldPosition;
        public Vector3 Pos1;
        public Vector3 Pos2;
        public Transform CallOutRectTf;
        public Image CallOutFrame;
        public bool IsUI = true;

        public void Reset()
        {
            OldPosition = default;
            Pos1 = default;
            Pos2 = default;
            CallOutRectTf = null;
            CallOutFrame = null;
            IsUI = true;
        }
    }
}
