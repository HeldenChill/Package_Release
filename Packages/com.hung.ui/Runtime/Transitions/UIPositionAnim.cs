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
        // PVM semantics: wait 5 frames before the FIRST play only (lets layout settle anchors).
        [SerializeField]
        bool waitFrame = false;
        bool hasWaitedFrame;
        Tween currentAnim;
        public override IReadOnlyList<UIAnim.Propertys> Datas => datas;

        public override void Play(ANIM anim)
        {
            // SHOW and HIDE reverse each other mid-flight from the current position (no snap);
            // any other overlap queues through the shared UIAnim guard.
            // Reverse only onto playable data; otherwise queue as before so the running anim still completes.
            bool reversing = IsReverse(state, anim) && CanPlay(anim);
            if (reversing)
            {
                currentAnim?.Kill();
                currentAnim = null;
                AbandonCurrent();
            }
            if (!TryBeginOrQueue(anim)) return;
            Propertys Data = Array.Find(datas, data => data != null && data.Id == anim);
            if (Data == null) return;
            if (tf == null) return;
            if (Data.StartTf == null || Data.EndTf == null)
            {
                Debug.LogError($"[UIPositionAnim] {name}: StartTf or EndTf is null for anim {anim}. Anchor destroyed?");
                return;
            }
            Data.OriginPos = tf.position;
            if (!reversing && Data.IsSetPositionToStart)
            {
                tf.position = Data.StartTf.position;
            }
            state = anim;
            int generation = CurrentGeneration;
            if (waitFrame && !hasWaitedFrame)
            {
                hasWaitedFrame = true;
                TimerManager.Ins.WaitForFrame(5, () => Run(anim, Data, generation));
            }
            else Run(anim, Data, generation);
        }

        bool CanPlay(ANIM anim)
        {
            if (tf == null) return false;
            Propertys Data = Array.Find(datas, data => data != null && data.Id == anim);
            return Data != null && Data.StartTf != null && Data.EndTf != null;
        }

        static bool IsReverse(ANIM running, ANIM requested) =>
            (running == ANIM.SHOW && requested == ANIM.HIDE) || (running == ANIM.HIDE && requested == ANIM.SHOW);

        void Run(ANIM anim, Propertys Data, int generation)
        {
            if (generation != CurrentGeneration || state != anim || tf == null) return;
            if (Data.EndTf == null)
            {
                state = ANIM.NONE;
                return;
            }
            OnAnimEnter((int)anim);
            currentAnim?.Kill();
            Tween tween = tf.DOMove(Data.EndTf.position, Data.Time).SetEase(Data.Ease).SetUpdate(true);
            if (anim == ANIM.IDLE) tween.SetLoops(2, LoopType.Yoyo);
            currentAnim = tween.OnComplete(() =>
            {
                if (generation != CurrentGeneration || state != anim) return;
                if (Data.IsReturnOriginPos) tf.position = Data.OriginPos;
                CompleteIfCurrent((int)anim, generation);
            });
        }

        public override void Stop()
        {
            currentAnim?.Kill();
            currentAnim = null;
            if (tf != null) tf.DOKill();
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
