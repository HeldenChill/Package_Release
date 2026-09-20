using UnityEngine;
using Hung.Utilities.Timer;

namespace Utilities
{
        public class Trigger
    {
        bool value;
        public bool Value
        {
            get => value;
            set
            {
                this.value = value;
                TimerManager.Ins.WaitForFrame(1, () => this.value = false);
            }
        }
    }
}
