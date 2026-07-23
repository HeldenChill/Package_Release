using System;
using System.Collections;
using System.Collections.Generic;
using Hung.UI;
using UnityEngine;

namespace Hung.Base
{
    public class UISButton : UIButton
    {
        [Serializable]
        protected class Propertys
        {
            public bool SetActiveByAnim = true;
        }
        [SerializeField]
        protected UIAnim[] anims;
        [SerializeField]
        protected Propertys data;
        protected int longestHideAnimId = 0;
        protected float longestHideAnimTime = 0;
        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i]._OnAnimEnter += OnAnimEnter;
                anims[i]._OnAnimExit += OnAnimExit;
                for (int j = 0; j < anims[i].Datas.Count; j++)
                {
                    UIAnim.Propertys Data = anims[i].Datas[j];
                    if (Data != null && Data.Id == UIAnim.ANIM.HIDE)
                    {
                        if (Data.Time > longestHideAnimTime)
                        {
                            longestHideAnimTime = Data.Time;
                            longestHideAnimId = anims[i].GetHashCode();
                        }
                        break;
                    }
                }
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i]._OnAnimEnter -= OnAnimEnter;
                anims[i]._OnAnimExit -= OnAnimExit;
            }
        }
        public virtual void Show()
        {
            gameObject.SetActive(true);
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].Play(UIAnim.ANIM.SHOW);
            }
        }

        public virtual void Hide()
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].Play(UIAnim.ANIM.HIDE);
            }
        }
        public virtual void PlayAnim(UIAnim.ANIM anim)
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].Play(anim);
            }
        }
        protected virtual void OnAnimEnter(int id, int anim)
        {
            button.interactable = false;
            if (data.SetActiveByAnim)
            {
                switch (anim)
                {
                    case (int)UIAnim.ANIM.SHOW:
                        if (!gameObject.activeInHierarchy)
                        {
                            gameObject.SetActive(true);
                        }
                        break;
                }
            }
        }
        protected virtual void OnAnimExit(int id, int anim)
        {
            button.interactable = true;
            if (data.SetActiveByAnim)
            {
                switch (anim)
                {
                    case (int)UIAnim.ANIM.HIDE:
                        if (id == longestHideAnimId)
                        {
                            if (data.SetActiveByAnim)
                            {
                                gameObject.SetActive(false);
                            }
                        }
                        break;
                }
            }
        }
    }
}
