using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.AutoTest
{
    [Serializable]
    public sealed class RuntimeSnapshot
    {
        public string runId;
        public string testId;
        public string sceneName;
        public int frame;
        public float time;
        public float realtime;
        public float elapsedSeconds;
        public CombatSnapshot combat = new CombatSnapshot();
        public DamageStatisticsSnapshotAdapter damageStatistics = new DamageStatisticsSnapshotAdapter();
        public List<EntitySnapshot> importantTransforms = new List<EntitySnapshot>();
        public List<PetSnapshot> pets = new List<PetSnapshot>();
        public List<EnemyStatusSnapshot> enemies = new List<EnemyStatusSnapshot>();
        public List<SupportSnapshot> supports = new List<SupportSnapshot>();
        public List<ProjectileSnapshot> projectiles = new List<ProjectileSnapshot>();
        public EventCountSnapshot events = new EventCountSnapshot();

        /// <summary>
        /// Host-owned payloads keyed by ordinal-unique ID, kept sorted by ID.
        /// Each entry stores a JsonUtility payload so the package never names a product type.
        /// See ADR-E5-autotest-runtime-snapshot-extensions.
        /// </summary>
        public List<RuntimeSnapshotExtension> extensions = new List<RuntimeSnapshotExtension>();

        /// <summary>Serializes <paramref name="value"/> under <paramref name="id"/>, replacing any existing entry.</summary>
        public void SetExtension<T>(string id, T value)
        {
            id = id != null ? id.Trim() : string.Empty;
            if (id.Length == 0)
                throw new ArgumentException("Extension ID must not be blank.", nameof(id));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            string json = JsonUtility.ToJson(value);
            RuntimeSnapshotExtension found = null;
            for (int i = 0; i < extensions.Count; i++)
            {
                RuntimeSnapshotExtension entry = extensions[i];
                if (entry != null && string.Equals(entry.id, id, StringComparison.Ordinal))
                {
                    if (found != null)
                        throw new InvalidOperationException("Duplicate snapshot extension ID: " + id);
                    found = entry;
                }
            }

            if (found == null)
                extensions.Add(new RuntimeSnapshotExtension { id = id, json = json });
            else
                found.json = json;

            extensions.Sort((left, right) => StringComparer.Ordinal.Compare(left != null ? left.id : null, right != null ? right.id : null));
        }

        /// <summary>Returns the raw payload only when exactly one non-empty entry matches.</summary>
        public bool TryGetExtensionJson(string id, out string json)
        {
            json = null;
            if (string.IsNullOrWhiteSpace(id) || extensions == null)
                return false;

            int matches = 0;
            for (int i = 0; i < extensions.Count; i++)
            {
                RuntimeSnapshotExtension entry = extensions[i];
                if (entry == null || !string.Equals(entry.id, id.Trim(), StringComparison.Ordinal))
                    continue;
                matches++;
                json = entry.json;
            }
            return matches == 1 && !string.IsNullOrEmpty(json);
        }

        /// <summary>Deserializes the payload; returns false when missing, duplicated, or malformed.</summary>
        public bool TryGetExtension<T>(string id, out T value)
        {
            value = default;
            if (!TryGetExtensionJson(id, out string json))
                return false;
            try
            {
                T parsed = JsonUtility.FromJson<T>(json);
                if (ReferenceEquals(parsed, null))
                    return false;
                value = parsed;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    /// <summary>One host-owned payload: an ordinal-unique ID and its JsonUtility JSON.</summary>
    [Serializable]
    public sealed class RuntimeSnapshotExtension
    {
        public string id;
        public string json;
    }

    [Serializable]
    public sealed class CombatSnapshot
    {
        public bool scenarioManagerFound;
        public bool scenarioAssigned;
        public bool isRunning;
        public int waveIndex;
        public int enemiesExpected;
        public int enemiesSpawned;
        public int enemiesKilled;
        public int enemiesAlive;
        public int wavesCompleted;
        public float totalDamage;
        public float dps;
        public string gameplayPhase;
        public bool scenarioApplying;
    }

    [Serializable]
    public sealed class DamageStatisticsSnapshotAdapter
    {
        public int totalRecordCalls;
        public int totalIgnoredCallsNoManager;
        public int totalIgnoredCallsNoSession;
        public float lastRecordTime;
        public float damageFromPetTowerManager;
    }

    [Serializable]
    public sealed class EntitySnapshot
    {
        public string name;
        public string path;
        public bool activeSelf;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool hasInvalidTransform;
    }

    [Serializable]
    public sealed class PetSnapshot
    {
        public string petId;
        public int level;
        public bool isAscended;
        public float atk;
        public float atkSpd;
        public float critRate;
        public float critDmg;
        public float atkRange;
        public Vector2Int gridPos;
        public Vector3 worldPos;
        public string fsmState;
        public string targetName;
        public bool hasTarget;
        public bool targetAlive;
        public float targetDistance;
        public bool isOutOfRange;
        public int detectionZoneCells;
    }

    [Serializable]
    public sealed class EnemyStatusSnapshot
    {
        public string enemyName;
        public float hp;
        public float moveSpeed;
        public float baseMoveSpeed;
        public List<string> activeTags = new List<string>();
        public int burnStacks;
        public int poisonStacks;
        public int coldStacks;
        public int shockStacks;
        public int bleedStacks;
        public bool hasOverload;
        public Vector2Int gridPos;
        public Vector3 position;
    }

    [Serializable]
    public sealed class SupportSnapshot
    {
        public string supportId;
        public float hp;
        public List<string> affectedPetIds = new List<string>();
    }

    [Serializable]
    public sealed class ProjectileSnapshot
    {
        public string prefabId;
        public bool hasBounce;
        public bool hasChain;
        public bool hasPierce;
        public bool hasAoE;
        public int activeCount;
    }

    [Serializable]
    public sealed class EventCountSnapshot
    {
        public List<string> statusTagCounts = new List<string>();
        public List<string> synergyTriggerCounts = new List<string>();
        public List<string> bounceCounts = new List<string>();
        public List<string> chainHopCounts = new List<string>();
        public List<string> aoeApplyCounts = new List<string>();
    }
}
