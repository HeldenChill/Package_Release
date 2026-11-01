using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Tidy top-down tree layout, as in the mockup: leaves left to right, each parent centred over its first and last child.</summary>
    public static class AIGraphLayout
    {
        public const float NodeWidth = 176f;
        public const float GapX = 22f;
        public const float RowHeight = 110f;

        /// <summary>Position for every node reachable from the root, then every parentless orphan tree to the right.</summary>
        public static Dictionary<string, Vector2> Compute(AIGraphAsset asset)
        {
            var result = new Dictionary<string, Vector2>();
            if (asset == null) return result;
            var visiting = new HashSet<string>();
            float cursor = 0f;
            AINode root = asset.GetNode(asset.RootNodeId);
            if (root != null) Place(asset, root, 0, ref cursor, result, visiting);
            foreach (AINode node in asset.Nodes)
            {
                if (node == null || result.ContainsKey(node.Id) || AIGraphEditing.ParentIdOf(asset, node.Id) != null) continue;
                Place(asset, node, 0, ref cursor, result, visiting);
            }
            return result;
        }

        static void Place(AIGraphAsset asset, AINode node, int depth, ref float cursor, Dictionary<string, Vector2> result, HashSet<string> visiting)
        {
            if (!visiting.Add(node.Id)) return;
            IReadOnlyList<string> ids = AIGraphAsset.ChildIdsOf(node);
            bool placedChild = false;
            float firstX = 0f;
            float lastX = 0f;
            for (int i = 0; i < ids.Count; i++)
            {
                AINode child = asset.GetNode(ids[i]);
                if (child == null || result.ContainsKey(child.Id)) continue;
                Place(asset, child, depth + 1, ref cursor, result, visiting);
                if (!result.TryGetValue(child.Id, out Vector2 position)) continue;
                if (!placedChild) firstX = position.x;
                lastX = position.x;
                placedChild = true;
            }
            float x;
            if (placedChild)
            {
                x = (firstX + lastX) * 0.5f;
            }
            else
            {
                x = cursor;
                cursor += NodeWidth + GapX;
            }
            result[node.Id] = new Vector2(x, depth * RowHeight);
        }
    }
}
