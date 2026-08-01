using System;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Hung.Utilities
{
    public class DetectMouse : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler
    {
        public event Action<PointerEventData> _OnPointerDown;
        public event Action<PointerEventData> _OnBeginDrag;
        public event Action<PointerEventData> _OnDrag;
        public event Action<PointerEventData> _OnPointerUp;

        public void OnPointerDown(PointerEventData eventData)
        {
            _OnPointerDown?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _OnBeginDrag?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _OnDrag?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _OnPointerUp?.Invoke(eventData);
        }
    }
}
