using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hung.AutoTest
{
    /// <summary>
    /// Owns UGUI EventSystem click and drag gesture sequencing for AutoTest cases.
    /// Accepts already-resolved targets and screen positions; performs no semantic target lookup.
    /// </summary>
    public sealed class AutoTestEventSystemPointerDriver
    {
        private const int MinimumDragFrames = 6;

        public string LastDiagnostic { get; private set; } = string.Empty;
        public bool IsDragging { get; private set; }

        private GameObject activeSource;
        private PointerEventData activeEventData;

        public bool TryClick(GameObject target, Vector2 screenPosition)
        {
            if (target == null)
            {
                LastDiagnostic = "Click target is null.";
                return false;
            }

            EventSystem eventSystem = ResolveEventSystem();
            if (eventSystem == null)
            {
                LastDiagnostic = "No active EventSystem.";
                return false;
            }

            var eventData = new PointerEventData(eventSystem) { position = screenPosition };

            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerClickHandler);

            LastDiagnostic = "Click dispatched.";
            return true;
        }

        public IEnumerator Drag(
            GameObject source,
            Vector2 startScreen,
            GameObject destination,
            Vector2 endScreen,
            int moveFrames)
        {
            if (source == null || destination == null)
            {
                LastDiagnostic = "Drag source or destination is null.";
                yield break;
            }

            EventSystem eventSystem = ResolveEventSystem();
            if (eventSystem == null)
            {
                LastDiagnostic = "No active EventSystem.";
                yield break;
            }

            int frames = Mathf.Max(moveFrames, MinimumDragFrames);
            var eventData = new PointerEventData(eventSystem) { position = startScreen };

            activeSource = source;
            activeEventData = eventData;
            IsDragging = true;

            ExecuteEvents.ExecuteHierarchy(source, eventData, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.ExecuteHierarchy(source, eventData, ExecuteEvents.beginDragHandler);

            for (int i = 1; i <= frames; i++)
            {
                float t = i / (float)frames;
                eventData.position = Vector2.Lerp(startScreen, endScreen, t);
                ExecuteEvents.ExecuteHierarchy(source, eventData, ExecuteEvents.dragHandler);
                yield return null;
            }

            ExecuteEvents.ExecuteHierarchy(destination, eventData, ExecuteEvents.dropHandler);
            ExecuteEvents.ExecuteHierarchy(source, eventData, ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(source, eventData, ExecuteEvents.pointerUpHandler);

            IsDragging = false;
            activeSource = null;
            activeEventData = null;
            LastDiagnostic = "Drag completed.";
        }

        public void Cancel()
        {
            if (!IsDragging || activeSource == null || activeEventData == null)
                return;

            ExecuteEvents.ExecuteHierarchy(activeSource, activeEventData, ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(activeSource, activeEventData, ExecuteEvents.pointerUpHandler);

            activeEventData.pointerPress = null;
            activeEventData.rawPointerPress = null;
            activeEventData.pointerDrag = null;

            IsDragging = false;
            activeSource = null;
            activeEventData = null;
            LastDiagnostic = "Drag canceled.";
        }

        private static EventSystem ResolveEventSystem()
        {
            if (EventSystem.current != null)
                return EventSystem.current;
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
#else
            return UnityEngine.Object.FindObjectOfType<EventSystem>(true);
#endif
        }
    }
}
