using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    public interface IAdsService : IAdsRequestService
    {
        IAds AppOpen { get; }
        IAds Banner { get; }
        IRewardAds Reward { get; }
        IInterAds Inter { get; }
        public ADS_TYPE Type { get; set; }
    }

    public interface IAds
    {
        public void Show();
        public void Hide();
        public void Load();
        public ADS_TYPE Type { get; set; }
    }
    public interface IRewardAds : IAds
    {
        public Action _OnTriggerLoadAds { get; }
        public Action<Action> _OnAddLoadAds { get; }
        public void Show(Action rewardCallBack, Action hiddenCallBack = null, Placement placement = Placement.NONE);
    }
    public interface IInterAds : IAds
    {
        public Action _OnTriggerLoadAds { get; }
        public Action<Action> _OnAddLoadAds { get; }
        public void Show(Action callback, Placement placement);
    }
    public interface IBannerAds : IAds
    {
        public void Show(ADS_TYPE type);
        public void Hide(ADS_TYPE type);
        public void Init();
    }
    
}
