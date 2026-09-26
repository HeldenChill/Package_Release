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
        private ANIM pendingTransitionAnim;
        private int animationGeneration;
        /// <summary>Changes whenever an animation starts or is interrupted.</summary>
        protected int CurrentGeneration => animationGeneration;
        internal bool IsPending(ANIM anim) => state == anim || (animQueue?.Contains(anim) ?? false);
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
            pendingTransitionAnim = ANIM.SHOW;
            Play(ANIM.SHOW);
        }

        public override void PlayOutro(Action onComplete)
        {
            pendingTransitionCallback = onComplete;
            pendingTransitionAnim = ANIM.HIDE;
            Play(ANIM.HIDE);
        }

        public override void Interrupt()
        {
            animationGeneration++;
            Stop();
            state = ANIM.NONE;
            animQueue?.Clear();
            pendingTransitionCallback = null;
            pendingTransitionAnim = ANIM.NONE;
        }

        /// <summary>
        /// Starts an idle animation or queues one distinct different request behind the active one.
        /// Closing while SHOW is in progress must not discard the HIDE request.
        /// </summary>
        protected bool TryBeginOrQueue(ANIM anim)
        {
            if (state == ANIM.NONE)
            {
                animationGeneration++;
                return true;
            }
            if (state == anim) return false;
            animQueue ??= new Queue<ANIM>();
            if (!animQueue.Contains(anim)) animQueue.Enqueue(anim);
            return false;
        }

        protected void OnAnimExit(int animCode)
        {
            int completedGeneration = animationGeneration;
            state = ANIM.NONE;
            Action callback = pendingTransitionAnim == (ANIM)animCode ? pendingTransitionCallback : null;
            if (callback != null)
            {
                pendingTransitionCallback = null;
                pendingTransitionAnim = ANIM.NONE;
            }
            _OnAnimExit?.Invoke(GetHashCode(), animCode);
            callback?.Invoke();
            if (completedGeneration == animationGeneration && animQueue?.Count > 0)
            {
                Play(animQueue.Dequeue());
            }
        }

        /// <summary>Ignore completion from an operation invalidated by interrupt or replay.</summary>
        protected void CompleteIfCurrent(int animCode, int generation)
        {
            if (generation == animationGeneration && state == (ANIM)animCode)
                OnAnimExit(animCode);
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
