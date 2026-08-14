using System;
using System.Collections;
using System.Collections.Generic;
using Hung.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Hung.Utilities.Timer;

namespace Hung.UI
{
    public class UIPositionAnim : UIAnim
    {
        [Serializable]
        private new class Propertys : UIAnim.Propertys
        {
            public Transform StartTf;
            public Transform EndTf;
            public bool IsSetPositionToStart = true;
            [HideInInspector]
            public Vector3 OriginPos;
            public bool IsReturnOriginPos = false;

        }
        [SerializeField]
        Transform tf;
        [SerializeField]
        Propertys[] datas;
        Tween currentAnim;
        public override IReadOnlyList<UIAnim.Propertys> Datas => datas;
        public override void Play(ANIM anim)
        {
            if (state != ANIM.NONE)
            {
                if (isAnimQueue)
                {
                    animQueue.Enqueue(anim);
                }
                return;
            }
            Propertys Data = Array.Find(datas, data => data.Id == anim);
            if (Data == null) return;
            if (tf == null) return;
            if (Data.StartTf == null || Data.EndTf == null)
            {
                Debug.LogError($"[UIPositionAnim] {name}: StartTf or EndTf is null for anim {anim}. Anchor destroyed?");
                return;
            }
            Data.OriginPos = tf.position;
            if (Data.IsSetPositionToStart)
            {
                tf.position = Data.StartTf.position;
            }
            state = anim;
            TimerManager.Ins.WaitForFrame(5, () =>
            {
                if (Data.EndTf == null)
                {
                    state = ANIM.NONE;
                    return;
                }
                switch (anim)
                {
                    case ANIM.SHOW:
                        OnAnimEnter((int)ANIM.SHOW);
                        currentAnim?.Kill();
                        currentAnim = tf.DOMove(Data.EndTf.position, Data.Time).SetEase(Data.Ease).OnComplete(
                            () =>
                            {
                                OnAnimExit((int)ANIM.SHOW);
                                if (Data.IsReturnOriginPos)
                                {
                                    transform.position = Data.OriginPos;
                                }
                            });
                        break;
                    case ANIM.HIDE:
                        OnAnimEnter((int)ANIM.HIDE);
                        currentAnim?.Kill();
                        currentAnim = tf.DOMove(Data.EndTf.position, Data.Time).SetEase(Data.Ease).OnComplete(() =>
                        {
                            OnAnimExit((int)ANIM.HIDE);
                            if (Data.IsReturnOriginPos)
                            {
                                transform.position = Data.OriginPos;
                            }
                        });
                        break;
                    case ANIM.IDLE:
                        OnAnimEnter((int)ANIM.IDLE);
                        currentAnim?.Kill();
                        currentAnim = tf.DOMove(Data.EndTf.position, Data.Time).SetEase(Data.Ease)
                        .SetLoops(2, LoopType.Yoyo).OnComplete(() =>
                        {
                            OnAnimExit((int)ANIM.IDLE);
                            if (Data.IsReturnOriginPos)
                            {
                                transform.position = Data.OriginPos;
                            }
                        });
                        break;
                }
            });

        }
        public override void Stop()
        {
            tf.DOKill();
            animQueue?.Clear();
        }
        [Button]
        public override void SetupBaseData()
        {
            tf ??= GetComponent<Transform>();
            tf = tf.Equals(null) ? GetComponent<Transform>() : tf;
            if (datas.Length < 2)
            {
                datas = new Propertys[2];
            }
            GameObject parent = new GameObject();
            parent.name = $"{name}Region";
            RectTransform parentRect = parent.AddComponent<RectTransform>();
            GameObject end = new GameObject();
            end.name = "End";
            RectTransform endRect = end.AddComponent<RectTransform>();
            GameObject start = new GameObject();
            start.name = "Start";
            RectTransform startRect = start.AddComponent<RectTransform>();
            parent.transform.parent = transform.parent;
            startRect.parent = transform.parent;
            endRect.parent = transform.parent;

            CopyRectTransform((RectTransform)tf, parentRect);
            CopyRectTransform((RectTransform)tf, startRect);
            CopyRectTransform((RectTransform)tf, endRect);
            transform.parent = parentRect;
            startRect.parent = parentRect;
            endRect.parent = parentRect;

            Propertys Data = Array.Find(datas, data => data?.Id == ANIM.SHOW);
            if (Data == null)
            {
                datas[0] = new Propertys()
                {
                    Id = ANIM.SHOW,
                    Time = 0.3f,
                    Ease = Ease.OutBack,
                    StartTf = startRect,
                    EndTf = endRect,
                };
            }
            else
            {
                datas[0].Id = ANIM.SHOW;
                datas[0].Time = 0.3f;
                datas[0].Ease = Ease.OutBack;
                datas[0].StartTf = startRect;
                datas[0].EndTf = endRect;
            }

            Propertys Data2 = Array.Find(datas, data => data?.Id == ANIM.HIDE);
            if (Data2 == null)
            {
                datas[1] = new Propertys()
                {
                    Id = ANIM.HIDE,
                    Time = 0.3f,
                    Ease = Ease.InBack,
                    StartTf = endRect,
                    EndTf = startRect,
                };
            }
            else
            {
                datas[1].Id = ANIM.HIDE;
                datas[1].Time = 0.3f;
                datas[1].Ease = Ease.InBack;
                datas[1].StartTf = endRect;
                datas[1].EndTf = startRect;
            }
        }
    }
}
