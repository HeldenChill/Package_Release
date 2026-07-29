using System;
using System.Collections.Generic;

namespace Hung.AutoTest
{
    /// <summary>
    /// Feeds gameplay events into the collector during a case. Game glue implements
    /// this (subscribing to its own event bus) and assigns EventSourceFactory —
    /// keeps the core collector free of game event types.
    /// </summary>
    public interface IAutoTestEventSource
    {
        void Start(AutoTestEventCollector collector);
        void Stop();
    }

    /// <summary>
    /// Counts gameplay events during one AutoTest case for assertions and snapshots.
    /// Generic channel/key counter store; the game's IAutoTestEventSource pushes into it.
    /// </summary>
    public sealed class AutoTestEventCollector
    {
        public const string StatusChannel = "status";
        public const string SynergyChannel = "synergy";

        /// <summary>Assigned by game glue (see PetVsMonsterAutoTestGlue). Null = no events collected.</summary>
        public static Func<IAutoTestEventSource> EventSourceFactory;

        readonly Dictionary<string, Dictionary<string, int>> counts = new Dictionary<string, Dictionary<string, int>>();
        IAutoTestEventSource source;

        public IReadOnlyDictionary<string, int> StatusAppliedByTag
        {
            get { return GetChannel(StatusChannel); }
        }

        public IReadOnlyDictionary<string, int> SynergyTriggersById
        {
            get { return GetChannel(SynergyChannel); }
        }

        public void Start()
        {
            Stop();
            source = EventSourceFactory != null ? EventSourceFactory() : null;
            if (source != null)
                source.Start(this);
        }

        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
                source = null;
            }
        }

        public void Clear()
        {
            counts.Clear();
        }

        public void Increment(string channel, string key)
        {
            if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(key))
                return;

            Dictionary<string, int> channelCounts = GetChannel(channel);
            channelCounts.TryGetValue(key, out int count);
            channelCounts[key] = count + 1;
        }

        public int GetCount(string channel, string key)
        {
            return !string.IsNullOrEmpty(key) && GetChannel(channel).TryGetValue(key, out int count)
                ? count
                : 0;
        }

        public int GetStatusAppliedCount(string tag)
        {
            return GetCount(StatusChannel, tag);
        }

        public int GetSynergyTriggerCount(string synergyId)
        {
            return GetCount(SynergyChannel, synergyId);
        }

        Dictionary<string, int> GetChannel(string channel)
        {
            if (!counts.TryGetValue(channel, out Dictionary<string, int> channelCounts))
            {
                channelCounts = new Dictionary<string, int>();
                counts[channel] = channelCounts;
            }
            return channelCounts;
        }
    }
}
