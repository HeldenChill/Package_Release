using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    using DG.Tweening;
    using Sirenix.OdinInspector;

    public class UIAlphaAnim : UIAnim
    {
        [SerializeField]
        CanvasGroup canvasGroup;
        [SerializeField]
        Propertys[] datas;

        public override IReadOnlyList<Propertys> Datas => datas;

        public override void Stop()
        {
            canvasGroup.DOKill();
        }
        public override void Play(ANIM anim)
        {
            if (!TryBeginOrQueue(anim)) return;
            Propertys Data = Array.Find(datas, data => data.Id == anim);
            if (Data == null) return;
            state = anim;
            int generation = CurrentGeneration;
            switch (anim)
            {
                case ANIM.SHOW:
                    OnAnimEnter((int)ANIM.SHOW);
                    canvasGroup.DOKill();
                    canvasGroup.alpha = 0;
                    canvasGroup.DOFade(1, Data.Time).SetEase(Data.Ease).OnComplete(() =>
                    {
                        CompleteIfCurrent((int)ANIM.SHOW, generation);
                    });
                    break;
                case ANIM.HIDE:
                    OnAnimEnter((int)ANIM.HIDE);
                    canvasGroup.DOFade(0, Data.Time).SetEase(Data.Ease).OnComplete(() =>
                    {
                        CompleteIfCurrent((int)ANIM.HIDE, generation);
                    });
                    break;


            }
        }
        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }
        [Button]
        public override void SetupBaseData()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            canvasGroup = canvasGroup.Equals(null) ? GetComponent<CanvasGroup>() : canvasGroup;
            if (datas.Length < 2)
            {
                datas = new Propertys[2];
            }
            Propertys Data = Array.Find(datas, data => data?.Id == ANIM.SHOW);
            if (Data == null)
            {
                datas[0] = new Propertys()
                {
                    Id = ANIM.SHOW,
                    Time = 0.3f,
                    Ease = Ease.OutQuad,
                };
            }
            else
            {
                datas[0].Id = ANIM.SHOW;
                datas[0].Time = 0.3f;
                datas[0].Ease = Ease.OutQuad;
            }

            Propertys Data2 = Array.Find(datas, data => data?.Id == ANIM.HIDE);
            if (Data2 == null)
            {
                datas[1] = new Propertys()
                {
                    Id = ANIM.HIDE,
                    Time = 0.3f,
                    Ease = Ease.InQuad,
                };
            }
            else
            {
                datas[1].Id = ANIM.HIDE;
                datas[1].Time = 0.3f;
                datas[1].Ease = Ease.InQuad;
            }
        }
    }
}
