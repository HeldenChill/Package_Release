using System.Collections;
using System.Threading.Tasks;
using Hung.Base;
using Hung.UI;
using UnityEngine;
using UnityEngine.Video;

namespace Hung.Tutorial
{
    public class TutorialUIController
    {
        private TutorialActionsCanvas _tutorialCanvas;
        private ShowContentPopup _showVideoPopup;
        private CallOutCanvas _callOutCanvas;

        public RectTransform CanvasRectTf => _tutorialCanvas != null ? _tutorialCanvas.RectTf : null;

        // GetUIAsync, not GetUI: a game may load its tutorial canvases from Addressables. The default
        // implementation resolves synchronously and invokes immediately, so nothing changes for games
        // whose canvases are already registry-resolvable.
        public void Acquire()
        {
            Locator.UI.GetUIAsync<TutorialActionsCanvas>(x => _tutorialCanvas = x);
            Locator.UI.GetUIAsync<CallOutCanvas>(x => _callOutCanvas = x);
            Locator.UI.GetUIAsync<ShowContentPopup>(x => _showVideoPopup = x);
        }

        public void CloseAll()
        {
            _tutorialCanvas?.Close();
            _showVideoPopup?.Close();
            _callOutCanvas?.Close();
        }

        public void PlayHand(TutorialDataSO.ActionData action)
        {
            _tutorialCanvas.Open(action);
            _tutorialCanvas.PlayAction(action.Action);
        }

        public IEnumerator ShowVideo(string description)
        {
            _showVideoPopup.Open(null);
            return _showVideoPopup.ShowVideo(description);
        }

        public void ShowContentItem(ItemId item)
        {
            _showVideoPopup.Open(null);
            _showVideoPopup.ShowContent(item);
        }

        public void ShowContentString(string content)
        {
            _showVideoPopup.Open(null);
            _showVideoPopup.ShowContent(content);
        }

        public void ShowCallOut(TutorialDataSO.ActionData action)
        {
            _callOutCanvas.Open(null);
            _callOutCanvas.Active(1);
            _callOutCanvas.Target(action.CallOutRectTf, action.Offset, action.Padding,
                action.CallOutFrame, isUI: action.IsUI);
        }

        public Task PrepareVideo(string videoName) => _showVideoPopup.PrepareVideo(videoName);
        public Task PrepareVideo(VideoClip clip) => _showVideoPopup.PrepareVideo(clip);
    }
}
