using UnityEngine;

namespace Hung.Base
{
    using Hung.UI;
    using UnityEngine.UI;
    using System.Collections.Generic;

    public interface IUIService
    {
        public RectTransform ParentCanvasTf { get; }
        public Canvas Canvas { get; }
        public CanvasScaler CanvasScaler { get; }
        public UIBackStack BackStack { get; }
        public void SetCameraScreenSpace(Camera cam);
        public T OpenUI<T>(object param = null) where T : UICanvas;
        public UICanvas OpenUIDirectly(UICanvas prefab, object param = null);
        public void CloseUI<T>() where T : UICanvas;
        public void ShowUI<T>() where T : UICanvas;
        public void HideUI<T>() where T : UICanvas;
        public bool IsOpened<T>() where T : UICanvas;
        public bool IsLoaded<T>() where T : UICanvas;
        public T GetUI<T>() where T : UICanvas;
        /// <summary>
        /// Resolves a canvas that may not be loaded yet, invoking <paramref name="onComplete"/> once it is.
        /// The default implementation resolves synchronously and invokes immediately; a game that loads its
        /// canvases asynchronously (Addressables, asset bundles) overrides it via <c>UIManager</c>'s partial
        /// hook. Callers must not assume the callback has already run when this returns.
        /// </summary>
        public void GetUIAsync<T>(System.Action<T> onComplete) where T : UICanvas;
        public void PreloadUI<T>() where T : UICanvas;
        public void UpdateAllUI();
        public void DestroyAllUI(HashSet<UICanvas> exception);
        public void HideAll();
        public void CloseAll();
    }
}
