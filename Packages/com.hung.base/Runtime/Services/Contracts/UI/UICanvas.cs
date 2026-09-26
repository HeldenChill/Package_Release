using Hung.Base;
using Hung.UI.Scoping;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hung.UI
{
    public abstract class UICanvas : MonoBehaviour, IScopedUi
    {
        /// <summary>Lifetime scope for this canvas. Existing canvases remain global by default.</summary>
        public virtual UiScope Scope => UiScope.Global;

        /// <summary>Registry used to bind an opened canvas to the current scope.</summary>
        protected virtual UiScopeRegistry ScopeRegistry => UiScopeRegistry.Instance;

        private int lifecycleVersion;

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
            lifecycleVersion++;
            if (Transition) Transition.Interrupt();
            Locator.UI.BackStack.Push(this, OnBackKey);
            OnOpen(param);
            UpdateUI();
            gameObject.SetActive(true);
            ScopeRegistry.Register(this);
            OnActivated();
            if (Transition) Transition.PlayIntro(null);
        }

        /// <summary>Closes this canvas when its bound scope is no longer active.</summary>
        public void OnScopeExit()
        {
            ScopeRegistry.Unregister(this);
            CloseCore(true);
        }

        public void Close() => CloseCore(false);

        private void CloseCore(bool scopeExit)
        {
            if (!scopeExit && !CanClose()) return;
            int version = ++lifecycleVersion;
            ScopeRegistry.Unregister(this);
            if (Transition)
            {
                Transition.Interrupt();
                Transition.PlayOutro(() => FinishClose(version));
            }
            else FinishClose(version);
        }

        private void FinishClose(int version)
        {
            if (version != lifecycleVersion) return;
            Locator.UI.BackStack.Remove(this);
            OnClose();
            if (version != lifecycleVersion) return;
            gameObject.SetActive(false);
            if (isDestroyOnClose) Destroy(gameObject);
        }

        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);

        protected virtual bool CanClose() => true;
        protected virtual void OnOpen(object param) { }
        /// <summary>Runs after the canvas becomes active and before its intro transition.</summary>
        protected virtual void OnActivated() { }
        protected virtual void OnClose() { }
        protected virtual void OnBackKey() { }
        public virtual void UpdateUI() { }

        protected virtual void OnDestroy()
        {
            ScopeRegistry.Unregister(this);
            if (Locator.UI != null) Locator.UI.BackStack.Remove(this);
        }
    }
}
