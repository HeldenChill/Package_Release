using System;
using UnityEngine;

namespace Hung.UI
{
    public abstract class UITransition : MonoBehaviour
    {
        public abstract void PlayIntro(Action onComplete);
        public abstract void PlayOutro(Action onComplete);
        public abstract void Interrupt();
    }
}
