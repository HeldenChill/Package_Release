using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Hung.Base;
using UnityEngine;
using UnityEngine.Audio;

namespace Hung.Audio
{
    /// <summary>Closed playback modes, independent of extensible cue identity.</summary>
    public enum AudioCueKind
    {
        /// <summary>A sound effect.</summary>
        Sfx = 0,
        /// <summary>Looping background music.</summary>
        Music = 1
    }

    /// <summary>Unity-serialized authoring row for one cue.</summary>
    [Serializable]
    public sealed class AudioCueDefinition
    {
        /// <summary>Stable identity.</summary>
        public AudioId Id;
        /// <summary>Generated C# member name.</summary>
        public string CodeName = string.Empty;
        /// <summary>Authored clip.</summary>
        public AudioClip Clip;
        /// <summary>Linear cue gain.</summary>
        [Range(0, 1)] public float Volume = 1f;
        /// <summary>Optional output routing.</summary>
        public AudioMixerGroup MixerGroup;
        /// <summary>Playback mode.</summary>
        public AudioCueKind Kind;
    }

    /// <summary>Copied runtime cue values; does not retain the mutable authoring row.</summary>
    public sealed class AudioCue
    {
        internal AudioCue(AudioCueDefinition row)
        {
            Id = row.Id; CodeName = row.CodeName; Clip = row.Clip;
            Volume = row.Volume; MixerGroup = row.MixerGroup; Kind = row.Kind;
        }
        /// <summary>Stable identity.</summary>
        public AudioId Id { get; }
        /// <summary>Generated member name.</summary>
        public string CodeName { get; }
        /// <summary>Authored clip reference.</summary>
        public AudioClip Clip { get; }
        /// <summary>Linear cue gain.</summary>
        public float Volume { get; }
        /// <summary>Optional output routing.</summary>
        public AudioMixerGroup MixerGroup { get; }
        /// <summary>Playback mode.</summary>
        public AudioCueKind Kind { get; }
    }

    /// <summary>Explicit cue catalog; validates completely before producing a runtime snapshot.</summary>
    [CreateAssetMenu(menuName = "Hung/Audio/Catalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [SerializeField] private List<AudioCueDefinition> entries = new List<AudioCueDefinition>();

        /// <summary>Returns a validated snapshot or throws with catalog and row identity.</summary>
        public IReadOnlyDictionary<AudioId, AudioCue> BuildIndex()
        {
            var index = new Dictionary<AudioId, AudioCue>();
            if (entries == null) throw new InvalidOperationException(name + ": entries is null.");
            for (int i = 0; i < entries.Count; i++)
            {
                var row = entries[i];
                string prefix = name + ": entry " + i + ": ";
                if (row == null || !row.Id.IsValid) throw new InvalidOperationException(prefix + "invalid ID.");
                if (string.IsNullOrWhiteSpace(row.CodeName)) throw new InvalidOperationException(prefix + "blank code name.");
                if (row.Clip == null) throw new InvalidOperationException(prefix + "missing clip.");
                if (float.IsNaN(row.Volume) || float.IsInfinity(row.Volume) || row.Volume < 0 || row.Volume > 1)
                    throw new InvalidOperationException(prefix + "invalid volume.");
                if (row.Kind != AudioCueKind.Sfx && row.Kind != AudioCueKind.Music)
                    throw new InvalidOperationException(prefix + "invalid kind.");
                bool music = row.Id.Value.StartsWith("bgm.", StringComparison.Ordinal);
                if (music != (row.Kind == AudioCueKind.Music))
                    throw new InvalidOperationException(prefix + "kind and bgm prefix disagree.");
                if (index.ContainsKey(row.Id)) throw new InvalidOperationException(prefix + "duplicate ID " + row.Id + ".");
                index.Add(row.Id, new AudioCue(row));
            }
            return new ReadOnlyDictionary<AudioId, AudioCue>(index);
        }
    }
}
