using Hung.Base;
using Hung.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.UI
{
    public class TutorialActionsCanvas : UISCanvas
    {
        [SerializeField]
        protected RectTransform handRectTf;
        [SerializeField]
        protected CanvasGroup handCanvasGroup;
        [SerializeField]
        protected RectTransform rect;
        protected TutorialDataSO.ActionData tutorialData;
        protected DG.Tweening.Sequence handSeq;

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            tutorialData = data as TutorialDataSO.ActionData;
            handRectTf.gameObject.SetActive(false);
        }
        public void PlayAction(HAND_ACTION action)
        {
            switch (action)
            {
                case HAND_ACTION.NONE:
                    break;
                case HAND_ACTION.POINT_OUT:
                    handRectTf.gameObject.SetActive(true);
                    handRectTf.localPosition = tutorialData.Pos1 - Vector3.up * 10;
                    handSeq = DOTween.Sequence();
                    handSeq.Append(handRectTf.DOLocalMove(tutorialData.Pos1 - Vector3.up * 30, 0.35f)
                    .SetEase(tutorialData.Ease))
                    .Append(handRectTf.DOLocalMove(tutorialData.Pos1 - Vector3.up * 10, 0.35f))
                    .SetLoops(-1, LoopType.Restart);
                    break;
                case HAND_ACTION.MOVE_DRAG:
                    handRectTf.gameObject.SetActive(true);
                    handRectTf.localPosition = tutorialData.Pos1;
                    handCanvasGroup.alpha = 0;
                    handSeq = DOTween.Sequence();
                    handSeq.Append(handCanvasGroup.DOFade(1, 0.3f))
                    .Append(handRectTf.DOLocalMove(tutorialData.Pos2, 1f)
                    .SetEase(tutorialData.Ease))
                    .Append(handCanvasGroup.DOFade(0, 0.3f))
                    .SetLoops(-1, LoopType.Restart);

                    break;
                case HAND_ACTION.ZOOM_IN:
                    break;
                case HAND_ACTION.ZOOM_OUT:
                    break;
            }
        }
        protected override void OnClose()
        {
            handSeq?.Kill();
            base.OnClose();
            tutorialData = null;
            handRectTf.gameObject.SetActive(false);
        }
    }
}
