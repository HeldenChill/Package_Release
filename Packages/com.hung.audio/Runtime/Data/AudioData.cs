using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hung.Audio
{
    using Hung.Base;
    [CreateAssetMenu(fileName = "AudioData", menuName = "ScriptableObjects/AudioData/AudioData", order = 1)]
    public class AudioData : SerializedScriptableObject
    {
        [Title("BGM")]
        [SerializeField] private readonly Dictionary<BGM_TYPE, Audio> _bgmAudioDict;

        [Title("SFX")]
        [SerializeField] private readonly Dictionary<SFX_TYPE, Audio> _sfxAudioDict;

        public Dictionary<BGM_TYPE, Audio> BgmAudioDict => _bgmAudioDict;

        public Dictionary<SFX_TYPE, Audio> SfxAudioDict => _sfxAudioDict;

        public List<Audio> BGMList;
        public List<Audio> SFXList;
    }

    [Serializable]
    public class Audio
    {
        public AudioClip clip;
        [Range(0, 1)]
        public float multiplier = 1;
    }
}