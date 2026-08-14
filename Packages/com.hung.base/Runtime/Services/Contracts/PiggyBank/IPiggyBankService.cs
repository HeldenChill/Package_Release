using UnityEngine;

namespace Hung.Base
{
    public interface IPiggyBankService
    {
        int PiggyGoldProgress { get; }
        void IncreasePiggyProgress();
        void ResetPiggyProgress();
    }
}
