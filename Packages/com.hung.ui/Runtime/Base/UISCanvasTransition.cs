using System;
using UnityEngine;

namespace Hung.UI
{
    [RequireComponent(typeof(UISCanvas))]
    public class UISCanvasTransition : UITransition
    {
        private UISCanvas _canvas;
        private int operationGeneration;
        private Action pendingIntro;
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
            // UISCanvas.OnActivated already starts SHOW once.
            if (onComplete == null) return;
            if (Canvas.IsFullyShown)
            {
                onComplete();
                return;
            }
            if (pendingIntro != null) Canvas.FullyShown -= pendingIntro;
            int generation = operationGeneration;
            Action handler = null;
            handler = () =>
            {
                Canvas.FullyShown -= handler;
                if (ReferenceEquals(pendingIntro, handler)) pendingIntro = null;
                if (generation == operationGeneration) onComplete();
            };
            pendingIntro = handler;
            Canvas.FullyShown += handler;
        }

        public override void PlayOutro(Action onComplete)
        {
            int generation = operationGeneration;
            Canvas.HideForClose(() =>
            {
                if (generation == operationGeneration) onComplete?.Invoke();
            });
        }

        public override void Interrupt()
        {
            operationGeneration++;
            if (pendingIntro != null)
            {
                Canvas.FullyShown -= pendingIntro;
                pendingIntro = null;
            }
            Canvas.StopAllAnims();
        }
    }
}
