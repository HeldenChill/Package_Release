using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Colors node views from a live AIAgent: the running path, and the result of nodes ticked in the last second. Also feeds the console log.</summary>
    public sealed class AIGraphDebugger
    {
        const double ResultFadeSeconds = 1.0;
        const int PulsePolls = 5;
        readonly AIGraphView view;
        readonly Dictionary<string, NodeStatus> lastStatus = new Dictionary<string, NodeStatus>();
        readonly Dictionary<string, double> lastTickTime = new Dictionary<string, double>();
        readonly Dictionary<string, string> titles = new Dictionary<string, string>();
        readonly Dictionary<string, string> parentTitles = new Dictionary<string, string>();
        readonly HashSet<string> activeIds = new HashSet<string>();
        AIAgent agent;
        AIContext subscribed;
        int pollCount;

        public AIGraphDebugger(AIGraphView view)
        {
            this.view = view;
        }

        public AIAgent Agent => agent;

        public AIDebugLog Log { get; } = new AIDebugLog();

        public bool IsLive => agent != null && EditorApplication.isPlaying && agent.IsInitialized && view.Asset != null && agent.Graph == view.Asset;

        public void SetAgent(AIAgent newAgent)
        {
            Unsubscribe();
            agent = newAgent;
            lastStatus.Clear();
            lastTickTime.Clear();
            titles.Clear();
            parentTitles.Clear();
            view.ClearDebug();
        }

        /// <summary>Called every 100 ms by the window.</summary>
        public void Update()
        {
            if (!IsLive)
            {
                if (subscribed != null)
                {
                    Unsubscribe();
                    view.ClearDebug();
                }
                return;
            }
            if (subscribed != agent.Context)
            {
                Unsubscribe();
                subscribed = agent.Context;
                subscribed.NodeTicked += OnNodeTicked;
                titles.Clear();
                parentTitles.Clear();
                Log.Clear();
                Log.Info("Runtime copy created for agent " + agent.name);
            }
            activeIds.Clear();
            IReadOnlyList<AINode> nodes = agent.RuntimeGraph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null && nodes[i].IsRunning) activeIds.Add(nodes[i].Id);
            pollCount++;
            bool dim = pollCount / PulsePolls % 2 == 1;
            double now = EditorApplication.timeSinceStartup;
            foreach (KeyValuePair<string, AINodeView> pair in view.NodeViews)
            {
                pair.Value.SetStatus(StatusClass(pair.Key, now));
                pair.Value.SetPulse(dim);
            }
            view.HighlightActiveEdges(activeIds);
        }

        public void Unsubscribe()
        {
            if (subscribed != null) subscribed.NodeTicked -= OnNodeTicked;
            subscribed = null;
        }

        /// <summary>inactive, running, success, failure or aborted, as the inspector shows it.</summary>
        public string StatusLabel(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || !IsLive) return "inactive";
            switch (StatusClass(nodeId, EditorApplication.timeSinceStartup))
            {
                case "ai-status-running": return "running";
                case "ai-status-success": return "success";
                case "ai-status-failure": return "failure";
                case "ai-status-aborted": return "aborted";
                default: return "inactive";
            }
        }

        string StatusClass(string nodeId, double now)
        {
            if (activeIds.Contains(nodeId)) return "ai-status-running";
            if (!lastStatus.TryGetValue(nodeId, out NodeStatus status) || now - lastTickTime[nodeId] > ResultFadeSeconds) return null;
            switch (status)
            {
                case NodeStatus.Success: return "ai-status-success";
                case NodeStatus.Failure: return "ai-status-failure";
                case NodeStatus.Aborted: return "ai-status-aborted";
                default: return null;
            }
        }

        void OnNodeTicked(AINode node, NodeStatus status)
        {
            lastStatus[node.Id] = status;
            lastTickTime[node.Id] = EditorApplication.timeSinceStartup;
            Log.Record(node.Id, TitleOf(node), ParentTitleOf(node.Id), status, Time.frameCount);
        }

        string TitleOf(AINode node)
        {
            if (titles.TryGetValue(node.Id, out string title)) return title;
            title = AINodeTypeCatalog.DisplayName(node);
            titles[node.Id] = title;
            return title;
        }

        /// <summary>Parent title from the design asset (runtime copy keeps node ids); empty for the root.</summary>
        string ParentTitleOf(string nodeId)
        {
            if (parentTitles.TryGetValue(nodeId, out string title)) return title;
            AIGraphAsset design = view.Asset;
            string parentId = design != null ? AIGraphEditing.ParentIdOf(design, nodeId) : null;
            AINode parent = parentId != null ? design.GetNode(parentId) : null;
            title = parent != null ? AINodeTypeCatalog.DisplayName(parent) : string.Empty;
            parentTitles[nodeId] = title;
            return title;
        }
    }
}
