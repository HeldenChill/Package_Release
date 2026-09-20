using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Deterministic order-invariant perceptual environment hash for AI.
/// Designed for runtime (no GC, no string, no sorting).
/// </summary>
public static class ENVIRONMENT_HASH
{
    // ==============================
    // CONFIGURATION
    // ==============================

    // Spatial quantization resolution (meters)
    // 10 = 0.1m, 2 = 0.5m, 1 = 1m
    private const float POSITION_Q = 10f;

    // Weight quantization resolution
    private const float WEIGHT_Q = 100f; // 0.01 precision

    // ==============================
    // QUANTIZATION
    // ==============================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int QPos(float v) => Mathf.RoundToInt(v * POSITION_Q);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int QWeight(float v) => Mathf.RoundToInt(v * WEIGHT_Q);

    // ==============================
    // HASH ENTRY (Position + Weight Binding)
    // ==============================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashEntry(Vector3 pos, float weight)
    {
        unchecked
        {
            // FNV-1a 64-bit
            ulong h = 1469598103934665603UL;

            h = (h ^ (ulong)QPos(pos.x)) * 1099511628211UL;
            h = (h ^ (ulong)QPos(pos.y)) * 1099511628211UL;
            h = (h ^ (ulong)QPos(pos.z)) * 1099511628211UL;
            h = (h ^ (ulong)QWeight(weight)) * 1099511628211UL;

            return h;
        }
    }

    // ==============================
    // MIX FUNCTION (Avalanche)
    // ==============================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix(ulong h)
    {
        unchecked
        {
            h ^= h >> 33;
            h *= 0xff51afd7ed558ccdUL;
            h ^= h >> 33;
            h *= 0xc4ceb9fe1a85ec53UL;
            h ^= h >> 33;
            return h;
        }
    }

    // ==============================
    // PUBLIC API
    // ==============================

    /// <summary>
    /// Compute environment perceptual hash.
    /// Order invariant, count sensitive, deterministic.
    /// </summary>
    public static ulong Compute(
        IList<Vector3> soundPositions,
        IList<float> soundWeights,
        IList<Vector3> seenPositions,
        IList<float> seenWeights)
    {
        ulong soundHash = Compute(soundPositions, soundWeights);
        ulong seenHash = Compute(seenPositions, seenWeights);
        // Combine sensory domains
        ulong envHash = Mix(soundHash ^ (seenHash << 1));
        return envHash;
    }
    public static ulong Compute(
        IList<Vector3> positions,
        IList<float> weights)
    {
        ulong hash = 0;
        int count = positions.Count;

        for (int i = 0; i < count; i++)
        {
            ulong h = HashEntry(positions[i], weights[i]);
            hash += Mix(h); // SUM = multiset commutative aggregation
        }


        // Combine sensory domains
        return hash;
    }
}