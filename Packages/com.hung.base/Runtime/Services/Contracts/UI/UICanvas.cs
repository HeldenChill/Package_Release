using Hung.Base;
using Hung.UI.Scoping;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hung.UI
{
    /// <summary>When an opened canvas joins the back stack.</summary>
    public enum UIBackPushPolicy
    {
        /// <summary>Every <see cref="UICanvas.Open"/> pushes. Package default.</summary>
        OnOpen,
        /// <summary>Only <see cref="UICanvas.Setup"/> pushes; <see cref="UICanvas.Open"/> never does.</summary>
        ExplicitSetup
    }

    public abstract class UICanvas : MonoBehaviour, IScopedUi
    {
        /// <summary>
        /// Process-wide back-stack policy. A game sets it once at composition, before any canvas opens.
        /// </summary>
        public static UIBackPushPolicy BackPushPolicy = UIBackPushPolicy.OnOpen;

        /// <summary>Lifetime scope for this canvas. Existing canvases remain global by default.</summary>
        public virtual UiScope Scope => UiScope.Global;

        /// <summary>Registry used to bind an opened canvas to the current scope.</summary>
        protected virtual UiScopeRegistry ScopeRegistry => UiScopeRegistry.Instance;

        private int lifecycleVersion;
        private bool closingForScopeExit;

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

        public virtual void Open(object param = null)
        {
            lifecycleVersion++;
            if (Transition) Transition.Interrupt();
            if (BackPushPolicy == UIBackPushPolicy.OnOpen) PushBack();
            OnOpen(param);
            UpdateUI();
            gameObject.SetActive(true);
            ScopeRegistry.Register(this);
            OnActivated();
            if (Transition) Transition.PlayIntro(null);
        }

        /// <summary>Adds this canvas to the back stack with <see cref="OnBackKey"/> as its action.</summary>
        public virtual void Setup(object param = null) => PushBack();

        private void PushBack() => Locator.UI?.BackStack.Push(this, OnBackKey);

        /// <summary>
        /// Closes this canvas when its bound scope is no longer active. Routes through the virtual
        /// <see cref="Close"/> so view overrides run, while still bypassing <see cref="CanClose"/>.
        /// </summary>
        public void OnScopeExit()
        {
            ScopeRegistry.Unregister(this);
            closingForScopeExit = true;
            try { Close(); }
            finally { closingForScopeExit = false; }
        }

        /// <summary>
        /// Closes with the outro transition. Scope exit also routes here, already unregistered, so an
        /// override must call <c>base.Close()</c> unconditionally and must not gate on
        /// <see cref="CanClose"/> itself (the base applies it, except on scope exit); skipping the base
        /// leaves the canvas open and never scope-closed again.
        /// </summary>
        public virtual void Close() => CloseCore(closingForScopeExit);

        /// <summary>
        /// Closes immediately without a transition and without requiring a matching Open. Does not
        /// raise <see cref="OnClose"/>. Invalidates any pending outro so it cannot hide a later reopen.
        /// </summary>
        public virtual void CloseDirectly(object param = null)
        {
            lifecycleVersion++;
            if (Transition) Transition.Interrupt();
            ScopeRegistry.Unregister(this);
            Locator.UI?.BackStack.Remove(this);
            Deactivate();
        }

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
            Locator.UI?.BackStack.Remove(this);
            OnClose();
            if (version != lifecycleVersion) return;
            Deactivate();
        }

        private void Deactivate()
        {
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
