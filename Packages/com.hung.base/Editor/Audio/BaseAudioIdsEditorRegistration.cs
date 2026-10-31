using Hung.Base;
using UnityEditor;

namespace Hung.Base.Editor
{
    [InitializeOnLoad]
    internal static class BaseAudioIdsEditorRegistration
    {
        static BaseAudioIdsEditorRegistration()
        {
            StableIdEditorRegistry<AudioId>.RegisterSource(new StableIdConstantsSource<AudioId>(typeof(BaseAudioIds)));
        }
    }
}
