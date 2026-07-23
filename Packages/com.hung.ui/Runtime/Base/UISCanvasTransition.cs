using System;
using UnityEngine;

namespace Hung.UI
{
    [RequireComponent(typeof(UISCanvas))]
    public class UISCanvasTransition : UITransition
    {
        private UISCanvas _canvas;
        // Lazy, not Awake-cached: this component is added at runtime via
        // AddComponent while its GameObject is still inactive (registry sets
        // SetActive(false) right after instantiate) - Unity defers Awake on
        // components added to inactive GameObjects, but Open() calls
        // Interrupt() on this transition immediately after adding it, before
        // the GameObject is ever reactivated. Awake-based caching left
        // `canvas` null for that first call.
        private UISCanvas Canvas => _canvas ? _canvas : (_canvas = GetComponent<UISCanvas>());

        public override void PlayIntro(Action onComplete)
        {
            Canvas.Show();
            onComplete?.Invoke();
        }

        public override void PlayOutro(Action onComplete)
        {
            Canvas.HideForClose(onComplete);
        }

        public override void Interrupt()
        {
            Canvas.StopAllAnims();
        }
    }
}
