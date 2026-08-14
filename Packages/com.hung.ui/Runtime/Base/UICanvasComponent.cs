using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    [RequireComponent(typeof(UICanvas))]
    public abstract class UICanvasComponent : MonoBehaviour
    {
        public abstract T Get<T>(int index) where T : MonoBehaviour;
        public virtual void Show() { }
        public virtual void Hide() { }
    }
}