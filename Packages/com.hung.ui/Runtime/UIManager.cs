using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hung.UI
{
    using Hung.Base;
    using Hung.DesignPattern;
    using UnityEngine.UI;

    /// <summary>
    /// Partial so a game can add its own UI helpers (screen-space math, banner insets, an
    /// addressable loader) from a <c>Hung.UI.asmref</c> folder without forking the manager -
    /// the same extension seam com.hung.data uses for <c>DataManager</c>.
    /// </summary>
    public partial class UIManager : Singleton<UIManager>, IUIService
    {
        [SerializeField]
        private RectTransform parentCanvasTf;
        [SerializeField]
        protected CanvasScaler canvasScaler;
        [SerializeField]
        private Canvas canvas;

        private IUIPrefabProvider prefabProvider;
        private CanvasRegistry registry;
        private UIBackStack backStack;

        public RectTransform ParentCanvasTf => parentCanvasTf;
        public Canvas Canvas => canvas;
        public CanvasScaler CanvasScaler => canvasScaler;
        public UIBackStack BackStack => backStack;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            prefabProvider = new ResourcesPrefabProvider();
            registry = new CanvasRegistry(prefabProvider, parentCanvasTf);
            backStack = new UIBackStack();
            Locator.UI = this;
        }

        public void SetCameraScreenSpace(Camera cam)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 10;
        }

        public T OpenUI<T>(object param = null) where T : UICanvas
        {
            T canvas = GetUI<T>();
            canvas.Open(param);
            return canvas;
        }

        public UICanvas OpenUIDirectly(UICanvas prefab, object param = null)
        {
            UICanvas canvas = Instantiate(prefab, parentCanvasTf);
            canvas.Open(param);
            return canvas;
        }

        public void HideUI<T>() where T : UICanvas
        {
            if (IsOpened<T>()) GetUI<T>().Hide();
        }

        public void ShowUI<T>() where T : UICanvas
        {
            if (!IsOpened<T>()) GetUI<T>().Show();
        }

        public void CloseUI<T>() where T : UICanvas
        {
            if (IsOpened<T>()) GetUI<T>().Close();
        }

        public bool IsOpened<T>() where T : UICanvas
        {
            return IsLoaded<T>() && registry.Get<T>().gameObject.activeInHierarchy;
        }

        public bool IsLoaded<T>() where T : UICanvas
        {
            return registry.IsLoaded<T>();
        }

        public T GetUI<T>() where T : UICanvas
        {
            return registry.Get<T>();
        }

        public void GetUIAsync<T>(System.Action<T> onComplete) where T : UICanvas
        {
            bool handled = false;
            TryGetUIAsyncOverride(onComplete, ref handled);
            if (handled) return;

            onComplete?.Invoke(GetUI<T>());
        }

        /// <summary>
        /// Optional per-game async canvas loader. A game whose canvases are not resolvable by the
        /// synchronous <see cref="registry"/> (e.g. they live in Addressables) implements this in its own
        /// <c>partial UIManager</c> and sets <paramref name="handled"/> to true; otherwise
        /// <see cref="GetUIAsync{T}"/> falls back to the synchronous path.
        /// </summary>
        partial void TryGetUIAsyncOverride<T>(System.Action<T> onComplete, ref bool handled) where T : UICanvas;

        public void PreloadUI<T>() where T : UICanvas
        {
            registry.Get<T>();
        }

        public void UpdateAllUI()
        {
            foreach (UICanvas ui in backStack.Canvases)
                ui.UpdateUI();
        }

        public void DestroyAllUI(HashSet<UICanvas> exception)
        {
            foreach (UICanvas ui in registry.All.ToList())
            {
                if (exception.Contains(ui)) continue;
                if (ui.gameObject.activeInHierarchy) ui.Close();
                registry.Remove(ui);
                Destroy(ui.gameObject);
            }
        }

        public void HideAll()
        {
            foreach (UICanvas ui in registry.All.Where(ui => ui != null && ui.gameObject.activeInHierarchy))
                ui.Hide();
        }

        public void CloseAll()
        {
            foreach (UICanvas ui in registry.All.Where(ui => ui != null && ui.gameObject.activeInHierarchy))
                ui.Close();
        }
    }
}
