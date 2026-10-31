using UnityEngine;

namespace Hung.Audio
{
    /// <summary>Explicit product configuration loaded only when automatic Audio is enabled.</summary>
    [CreateAssetMenu(menuName = "Hung/Audio/Plugin Configuration", fileName = "HungAudio")]
    public sealed class AudioPluginConfiguration : ScriptableObject
    {
        [SerializeField] private AudioCatalog catalog;
        [SerializeField, Range(1, 128)] private int maxSfxVoices = 16;
        /// <summary>Product-owned cue catalog.</summary>
        public AudioCatalog Catalog => catalog;
        /// <summary>Maximum simultaneous effect voices.</summary>
        public int MaxSfxVoices => maxSfxVoices;
    }
}
