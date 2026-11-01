using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hung.Base;
using Hung.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Utilities;
using Hung.Utilities.Timer;

namespace Hung.UI
{
    public class ShowContentPopup : BasePopup
    {
        [SerializeField]
        GameObject mainPopup;
        [SerializeField]
        GameObject smallPopup;
        [SerializeField]
        GameObject backGround;
        [SerializeField]
        VideoPlayer videoPlayer;
        [SerializeField]
        UIButton confirmButton;
        [SerializeField]
        TMP_Text descriptionTxt;
        [SerializeField]
        TMP_Text smallDescriptionTxt;
        [SerializeField]
        GameObject videoParent;
        [SerializeField]
        GameObject explainContent;
        [SerializeField]
        List<GameObject> hintObjs;
        [SerializeField]
        List<GameObject> knifeObjs;
        [SerializeField]
        List<RectTransform> layoutElements;
        [SerializeField]
        float minCloseTime = 1f;
        STimer cdTimer;
        bool isOpen = false;
        Trigger isClose = new Trigger(1);

        protected void Awake()
        {
            cdTimer = TimerManager.Ins.PopSTimer();
            confirmButton._OnClick += OnConfirmButtonClick;
            Locator.Tutorial?.AddCustomCondition(TutorialKeys.ConfirmButtonClick, IsConfirmButtonClicked);
            Locator.Tutorial?.AddCustomCondition(TutorialKeys.VideoPopupClose, isClose.GetValue);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            confirmButton._OnClick -= OnConfirmButtonClick;
            Locator.Tutorial?.RemoveCustomCondition(TutorialKeys.ConfirmButtonClick);
            Locator.Tutorial?.RemoveCustomCondition(TutorialKeys.VideoPopupClose);
        }
        protected override void OnOpen(object param)
        {
            base.OnOpen(param);
            cdTimer.Start(minCloseTime, () => confirmButton.gameObject.SetActive(true));
            confirmButton.gameObject.SetActive(false);
            isOpen = true;
            DevLog.Log(DevId.System, "OPEN - VIDEO");
        }
        protected override bool CanClose() => isOpen && !(mainPopup.activeInHierarchy && cdTimer.IsStart);
        protected override void OnClose()
        {
            isOpen = false;
            videoPlayer.Stop();
            base.OnClose();
            DevLog.Log(DevId.System, "CLOSE - VIDEO");
        }
        protected override void OnAnimExit(int id, int anim)
        {
            base.OnAnimExit(id, anim);
            switch (anim)
            {
                case (int)UIAnim.ANIM.HIDE:
                    if (id == longestHideAnimId)
                    {
                        isClose.Value = true;
                    }
                    break;
            }
        }
        public async Task PrepareVideo(VideoClip clip)
        {
            videoPlayer.clip = clip;
            videoPlayer.isLooping = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = false;
            gameObject.SetActive(true);
            videoParent.SetActive(true);
            backGround.SetActive(true);
            smallPopup.SetActive(false);
            mainPopup.SetActive(true);
            videoParent.SetActive(true);
            for (int i = 0; i < layoutElements.Count; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutElements[i]);
            }
            transform.localScale = Vector3.zero;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
                await Task.Yield();
            if (videoPlayer.canStep)
                videoPlayer.StepForward();
            await Task.Yield();
            videoPlayer.Pause();
        }
        public async Task PrepareVideo(string clipName)
        {
            string path = $"{Application.streamingAssetsPath}/Tutorial/{clipName}";
            videoPlayer.url = path;
            videoPlayer.isLooping = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = false;
            gameObject.SetActive(true);
            videoParent.SetActive(true);
            backGround.SetActive(true);
            smallPopup.SetActive(false);
            mainPopup.SetActive(true);
            videoParent.SetActive(true);
            for (int i = 0; i < layoutElements.Count; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutElements[i]);
            }
            transform.localScale = Vector3.zero;
            await Task.Yield();
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
                await Task.Yield();
            if (videoPlayer.canStep)
                videoPlayer.StepForward();
            await Task.Yield();
            videoPlayer.Pause();
        }
        public virtual IEnumerator ShowVideo(string description = null)
        {
            transform.localScale = Vector3.one;
            descriptionTxt.text = description;
            foreach (GameObject obj in knifeObjs)
            {
                obj.SetActive(false);
            }
            foreach (GameObject obj in hintObjs)
            {
                obj.SetActive(false);
            }
            explainContent.SetActive(false);
            for (int i = 0; i < layoutElements.Count; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutElements[i]);
            }

            while (!videoPlayer.isPrepared)
                yield return null;
            DevLog.Log(DevId.System, $"Play Videos - {videoPlayer.clip} - {videoPlayer.url}");
            videoPlayer.SetDirectAudioMute(0, true);
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        public virtual void ShowContent(ItemId item)
        {
            mainPopup.SetActive(true);
            smallPopup.SetActive(false);
            videoParent.SetActive(false);
            explainContent.SetActive(true);
            backGround.SetActive(true);
            if (item == BaseItemIds.HintBooster)
            {
                foreach (GameObject obj in knifeObjs)
                {
                    obj.SetActive(false);
                }
                foreach (GameObject obj in hintObjs)
                {
                    obj.SetActive(true);
                }
            }
            else if (item == BaseItemIds.KnifeBooster)
            {
                foreach (GameObject obj in knifeObjs)
                {
                    obj.SetActive(true);
                }
                foreach (GameObject obj in hintObjs)
                {
                    obj.SetActive(false);
                }
            }
            for (int i = 0; i < layoutElements.Count; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutElements[i]);
            }
        }

        public virtual void ShowContent(string content)
        {
            mainPopup.SetActive(false);
            smallPopup.SetActive(true);
            backGround.SetActive(false);
            smallDescriptionTxt.text = content;
        }

        public virtual void SetDescription(string description)
        {
            descriptionTxt.text = description;
        }
        private void OnConfirmButtonClick(int obj)
        {
            Close();
        }
        protected bool IsConfirmButtonClicked()
        {
            return !isOpen;
        }
        
    }
}
