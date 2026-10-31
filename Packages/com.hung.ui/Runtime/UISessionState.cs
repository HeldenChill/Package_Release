using UnityEngine;

namespace Hung.UI
{
    // Reset before product BeforeSceneLoad/scene composition, including reload-disabled play.
    internal static class UISessionState
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay()
        {
            UICanvas.BackPushPolicy = UIBackPushPolicy.OnOpen;
            UIButton.DefaultClickSfx = true;
            UISCanvas.ResetSessionObservers();
        }
    }
}
