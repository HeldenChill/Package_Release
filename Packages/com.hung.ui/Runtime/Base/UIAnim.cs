using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    using DG.Tweening;
    using System;
    public abstract class UIAnim : UITransition
    {
        public event Action<int, int> _OnAnimExit;
        public event Action<int, int> _OnAnimEnter;
        [Serializable]
        public class Propertys
        {
            public ANIM Id;
            public float Time = 0.5f;
            public Ease Ease;
        }
        public enum ANIM
        {
            NONE = 0,
            SHOW = 1,
            HIDE = 2,
            IDLE = 3,
        }
        public virtual IReadOnlyList<Propertys> Datas
        {
            get;
        }

        protected ANIM state;
        [SerializeField]
        protected bool isAnimQueue = false;
        protected Queue<ANIM> animQueue;
        protected const int LEFT_X = -850;
        protected const int UP_Y = 850;
        private Action pendingTransitionCallback;
        public virtual void OnInit() { }
        public virtual void Play() { }
        public virtual void Play(ANIM anim) { }
        public abstract void Stop();
        protected virtual void Awake()
        {
            if (isAnimQueue)
            {
                animQueue = new Queue<ANIM>();
            }
        }

        public override void PlayIntro(Action onComplete)
        {
            pendingTransitionCallback = onComplete;
            Play(ANIM.SHOW);
        }

        public override void PlayOutro(Action onComplete)
        {
            pendingTransitionCallback = onComplete;
            Play(ANIM.HIDE);
        }

        public override void Interrupt()
        {
            Stop();
            state = ANIM.NONE;
            pendingTransitionCallback = null;
        }

        protected void OnAnimExit(int animCode)
        {
            _OnAnimExit?.Invoke(GetHashCode(), animCode);
            state = ANIM.NONE;
            Action callback = pendingTransitionCallback;
            pendingTransitionCallback = null;
            callback?.Invoke();
            if (isAnimQueue && animQueue?.Count > 0)
            {
                Play(animQueue.Dequeue());
            }
        }

        protected void OnAnimEnter(int animCode)
        {
            _OnAnimEnter?.Invoke(GetHashCode(), animCode);
        }

        public virtual void SetupBaseData()
        {

        }
        public static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;

            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;

            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;

            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }
    }
}