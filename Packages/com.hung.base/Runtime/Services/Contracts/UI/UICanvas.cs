using Hung.Base;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hung.UI
{
    public abstract class UICanvas : MonoBehaviour
    {
        [FormerlySerializedAs("IsDestroyOnClose")]
        public bool isDestroyOnClose;

        [SerializeField] private RectTransform rectTf;
        public RectTransform RectTf => rectTf;

        private Canvas _canvas;
        public Canvas Canvas => _canvas = _canvas ? _canvas : GetComponent<Canvas>();

        private UITransition _transition;
        private bool _transitionCached;
        private UITransition Transition
        {
            get
            {
                if (!_transitionCached) { _transition = EnsureTransition(); _transitionCached = true; }
                return _transition;
            }
        }

        // Lazy, virtual-dispatch factory hook — deliberately NOT resolved in Awake().
        // Many existing canvas subclasses declare their own non-override Awake(),
        // which hides a base Awake() in C#; a self-add-on-Awake approach would
        // silently never run for them. This runs on first Open/Close instead.
        protected virtual UITransition EnsureTransition() => GetComponent<UITransition>();

        public void Open(object param = null)
        {
            if (Transition) Transition.Interrupt();
            Locator.UI.BackStack.Push(this, OnBackKey);
            OnOpen(param);
            UpdateUI();
            gameObject.SetActive(true);
            if (Transition) Transition.PlayIntro(null);
        }

        public void Close()
        {
            if (!CanClose()) return;
            if (Transition) { Transition.Interrupt(); Transition.PlayOutro(FinishClose); }
            else FinishClose();
        }

        private void FinishClose()
        {
            Locator.UI.BackStack.Remove(this);
            OnClose();
            gameObject.SetActive(false);
            if (isDestroyOnClose) Destroy(gameObject);
        }

        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);

        protected virtual bool CanClose() => true;
        protected virtual void OnOpen(object param) { }
        protected virtual void OnClose() { }
        protected virtual void OnBackKey() { }
        public virtual void UpdateUI() { }
    }
}
