using UnityEngine;

namespace Hung.Base
{
    public interface ISpinWheelService 
    {
        bool IsDoneSpinFreeToday { get; }
        int AdsSpinToday { get; }
        int DayOfYear { get; }
        void ResetSpinData();
        void SaveItem(SpinItem wheelSpinValue);
    }
}
