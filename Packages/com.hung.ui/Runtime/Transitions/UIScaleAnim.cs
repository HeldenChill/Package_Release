using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    using DG.Tweening;
    using Sirenix.OdinInspector;

    public class UIScaleAnim : UIAnim
    {
        [Serializable]
        private new class Propertys : UIAnim.Propertys
        {
            public Vector2 StartSize = Vector3.zero;
            public Vector2 EndSize;
            public bool IsSetStartSize = false;
        }
        [SerializeField]
        RectTransform RectTransform;
        [SerializeField]
        Propertys[] datas;
        public override IReadOnlyList<UIAnim.Propertys> Datas => datas;
        public override void Stop()
        {
            RectTransform.DOKill();
        }
        [Button]
        public override void Play(ANIM anim)
        {
            if (state != ANIM.NONE) return;
            Propertys Data = Array.Find(datas, data => data.Id == anim);
            if (Data == null) return;
            state = anim;
            if (Data.IsSetStartSize)
            {
                RectTransform.localScale = Data.StartSize;
            }
            switch (anim)
            {
                case ANIM.SHOW:
                    OnAnimEnter((int)ANIM.SHOW);
                    RectTransform.DOScale(Data.EndSize, Data.Time).SetEase(Data.Ease).OnComplete(
                        () =>
                        {
                            OnAnimExit((int)ANIM.SHOW);
                            state = ANIM.NONE;
                        });
                    break;
                case ANIM.HIDE:
                    OnAnimEnter((int)ANIM.HIDE);
                    RectTransform.DOScale(Data.EndSize, Data.Time).SetEase(Data.Ease).OnComplete(() =>
                    {
                        OnAnimExit((int)ANIM.HIDE);
                        state = ANIM.NONE;
                    });
                    break;
            }
        }
        
        [Button]
        public override void SetupBaseData()
        {
            RectTransform ??= GetComponent<RectTransform>();
            RectTransform = RectTransform.Equals(null) ? GetComponent<RectTransform>() : RectTransform;
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