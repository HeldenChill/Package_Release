using System.Collections.Generic;
using Hung.Base;
using UnityEditor;
using UnityEngine;

namespace Hung.Tutorial.EditorTools
{
    public static class TutorialGraphMigration
    {
        private const float BaseNodeW = 220f;
        private const float BaseMargin = 72f;
        private const float BaseRankGapY = 178f;
        private const float BaseSiblingGapX = 290f;

        [MenuItem("Tools/Tutorial/Migrate Linear Data To Graph")]
        public static void Migrate()
        {
            var src = Selection.activeObject as TutorialDataSO;
            if (src == null)
            {
                EditorUtility.DisplayDialog("Tutorial Migration",
                    "Select a TutorialDataSO asset first.", "OK");
                return;
            }

            foreach (var kvp in src.TutorialDatas)
            {
                int level = kvp.Key;
                var data = kvp.Value;
                var graph = ScriptableObject.CreateInstance<TutorialGraphSO>();
                graph.tutorialLevel = level;

                var idByStep = new List<int>();
                for (int i = 0; i < data.ConditionActions.Count; i++)
                {
                    var ca = data.ConditionActions[i];
                    var node = new TutorialGraphSO.TutorialStepNode
                    {
                        id = graph.AllocId(),
                        tutorialLevel = level,
                        tutorialStepIndex = i,
                        layer = i,
                        slot = 0,
                        conditionKey = ca.Condition,
                        actionType = ca.Action.Type,
                        action = ca.Action,
                        delay = ca.Action.Delay,
                        postProcessKeys = new List<string>(ca.Action.PostProcessActions),
                        otherActionKeys = new List<string>(ca.Action.OtherActions),
                    };
                    graph.nodes.Add(node);
                    idByStep.Add(node.id);
                }
                if (graph.nodes.Count > 0) graph.startNodeId = idByStep[0];

                for (int i = 0; i < data.ConditionActions.Count; i++)
                {
                    var node = graph.nodes[i];
                    var action = data.ConditionActions[i].Action;
                    if (action.Type == TUTORIAL_ACTION.BRANCH && action.AdvanceSteps != null)
                    {
                        foreach (int adv in action.AdvanceSteps)
                        {
                            int target = i + adv;
                            if (target >= 0 && target < idByStep.Count && !node.nextIds.Contains(idByStep[target]))
                                node.nextIds.Add(idByStep[target]);
                        }
                    }
                    else if (i + 1 < idByStep.Count)
                    {
                        node.nextIds.Add(idByStep[i + 1]);
                    }
                }

                ApplyReadableLayout(graph);

                string path = $"Assets/_Game/_Tutorial/Data/TutorialGraph_Level{level}.asset";
                AssetDatabase.CreateAsset(graph, AssetDatabase.GenerateUniqueAssetPath(path));
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Tutorial Migration",
                "Migration complete. Generated graph assets now include a readable saved editor layout.",
                "OK");
        }

        private static void ApplyReadableLayout(TutorialGraphSO graph)
        {
            if (graph == null || graph.nodes.Count == 0) return;

            var backEdges = FindBackEdges(graph);
            var rankMap = CalculateRanks(graph, backEdges);
            var byRank = new Dictionary<int, List<TutorialGraphSO.TutorialStepNode>>();
            foreach (var node in graph.nodes)
            {
                int rank = rankMap.ContainsKey(node.id) ? rankMap[node.id] : 0;
                if (!byRank.ContainsKey(rank)) byRank[rank] = new List<TutorialGraphSO.TutorialStepNode>();
                byRank[rank].Add(node);
            }

            int maxRank = 0;
            int maxCount = 1;
            foreach (var kvp in byRank)
            {
                maxRank = Mathf.Max(maxRank, kvp.Key);
                maxCount = Mathf.Max(maxCount, kvp.Value.Count);
                kvp.Value.Sort((a, b) => a.id.CompareTo(b.id));
            }

            float centerX = BaseMargin + BaseNodeW * 0.5f + Mathf.Max(0, maxCount - 1) * BaseSiblingGapX * 0.5f;
            for (int rank = 0; rank <= maxRank; rank++)
            {
                if (!byRank.ContainsKey(rank)) continue;
                var row = byRank[rank];
                for (int i = 0; i < row.Count; i++)
                {
                    var node = row[i];
                    node.layer = rank;
                    node.slot = i;
                    node.useManualPosition = true;
                    node.editorPosition = new Vector2(
                        centerX + (i - (row.Count - 1) * 0.5f) * BaseSiblingGapX - BaseNodeW * 0.5f,
                        BaseMargin + rank * BaseRankGapY);
                }
            }
        }

        private static Dictionary<int, int> CalculateRanks(TutorialGraphSO graph, HashSet<EdgeKey> backEdges)
        {
            var rank = new Dictionary<int, int>();
            var inDegree = new Dictionary<int, int>();
            var adj = new Dictionary<int, List<int>>();

            foreach (var node in graph.nodes)
            {
                rank[node.id] = 0;
                inDegree[node.id] = 0;
                adj[node.id] = new List<int>();
            }

            foreach (var node in graph.nodes)
            {
                foreach (int childId in node.nextIds)
                {
                    if (!inDegree.ContainsKey(childId)) continue;
                    if (backEdges.Contains(new EdgeKey(node.id, childId))) continue;
                    adj[node.id].Add(childId);
                    inDegree[childId]++;
                }
            }

            var queue = new Queue<int>();
            if (graph.GetNode(graph.startNodeId) != null) queue.Enqueue(graph.startNodeId);
            foreach (var node in graph.nodes)
                if (inDegree[node.id] == 0 && node.id != graph.startNodeId) queue.Enqueue(node.id);

            var visited = new HashSet<int>();
            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                visited.Add(id);
                foreach (int childId in adj[id])
                {
                    rank[childId] = Mathf.Max(rank[childId], rank[id] + 1);
                    inDegree[childId]--;
                    if (inDegree[childId] == 0) queue.Enqueue(childId);
                }
            }

            int fallback = rank.Count;
            foreach (var node in graph.nodes)
                if (!visited.Contains(node.id)) rank[node.id] = fallback++;

            return rank;
        }

        private static HashSet<EdgeKey> FindBackEdges(TutorialGraphSO graph)
        {
            var result = new HashSet<EdgeKey>();
            var visited = new HashSet<int>();
            var stack = new HashSet<int>();

            int startId = graph.GetNode(graph.startNodeId) != null ? graph.startNodeId : (graph.nodes.Count > 0 ? graph.nodes[0].id : -1);
            if (startId >= 0) Dfs(startId);
            foreach (var node in graph.nodes)
                if (!visited.Contains(node.id)) Dfs(node.id);

            void Dfs(int id)
            {
                if (visited.Contains(id)) return;
                visited.Add(id);
                stack.Add(id);

                var node = graph.GetNode(id);
                if (node != null)
                {
                    foreach (int childId in node.nextIds)
                    {
                        if (stack.Contains(childId)) result.Add(new EdgeKey(id, childId));
                        else if (!visited.Contains(childId)) Dfs(childId);
                    }
                }

                stack.Remove(id);
            }

            return result;
        }

        private struct EdgeKey
        {
            private readonly int _from;
            private readonly int _to;

            public EdgeKey(int from, int to)
            {
                _from = from;
                _to = to;
            }

            public override int GetHashCode() => (_from * 397) ^ _to;
            public override bool Equals(object obj) => obj is EdgeKey other && other._from == _from && other._to == _to;
        }
    }
}
