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
        public const string BounceChannel = "bounce";
        public const string ChainChannel = "chain";
        public const string AoEChannel = "aoe";
        public const string SpellCastChannel = "spellcast";
        public const string RiotDashStepChannel = "riotdashstep";

        /// <summary>Assigned by game glue. Null means no events are collected.</summary>
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

        public IReadOnlyDictionary<string, int> BouncesByPrefab
        {
            get { return GetChannel(BounceChannel); }
        }

        public IReadOnlyDictionary<string, int> ChainHopsByPrefab
        {
            get { return GetChannel(ChainChannel); }
        }

        public IReadOnlyDictionary<string, int> AoEAppliesByPrefab
        {
            get { return GetChannel(AoEChannel); }
        }

        public IReadOnlyDictionary<string, int> SpellCastsByIndex
        {
            get { return GetChannel(SpellCastChannel); }
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

        /// <summary>
        /// Returns a specific key's count when supplied, or the complete channel total.
        /// This keeps consumers independent of the game-specific event key vocabulary.
        /// </summary>
        public int GetCountOrTotal(string channel, string key)
        {
            if (!string.IsNullOrEmpty(key))
                return GetCount(channel, key);

            int total = 0;
            foreach (KeyValuePair<string, int> pair in GetChannel(channel))
                total += pair.Value;
            return total;
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
