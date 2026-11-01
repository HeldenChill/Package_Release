using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>One node in the graph view, drawn as the mockup's card. Holds only the node id; node data is always read from the asset by id.</summary>
    public sealed class AINodeView : Node
    {
        const string RunningClass = "ai-status-running";
        readonly Action selectionChanged;
        readonly VisualElement ring;
        readonly Label categoryLabel;
        readonly Label rootLabel;
        readonly Label badge;
        readonly VisualElement dot;
        readonly Label nameLabel;
        readonly Label subtitle;
        readonly Label abortChip;
        string statusClass;

        public AINodeView(string nodeId, AINodeCategory category, IEdgeConnectorListener listener, Action onSelectionChanged)
        {
            NodeId = nodeId;
            Category = category;
            selectionChanged = onSelectionChanged;
            viewDataKey = nodeId;
            AddToClassList("ai-node");
            AddToClassList("ai-" + category.ToString().ToLowerInvariant());
            Input = AIPort.Create(Direction.Input, Port.Capacity.Single, listener);
            inputContainer.Add(Input);
            if (category == AINodeCategory.Composite) Output = AIPort.Create(Direction.Output, Port.Capacity.Multi, listener);
            else if (category == AINodeCategory.Decorator) Output = AIPort.Create(Direction.Output, Port.Capacity.Single, listener);
            if (Output != null) outputContainer.Add(Output);
            RefreshPorts();
            RefreshExpandedState();
            // Status ring sits behind the card, 4px outside it.
            ring = new VisualElement { pickingMode = PickingMode.Ignore };
            ring.AddToClassList("ai-node-ring");
            Insert(0, ring);
            // GraphView's own title bar and contents are hidden; the card is built inside the border element.
            titleContainer.style.display = DisplayStyle.None;
            VisualElement contents = mainContainer.Q("contents");
            if (contents != null) contents.style.display = DisplayStyle.None;
            var head = new VisualElement();
            head.AddToClassList("ai-node-head");
            categoryLabel = new Label(category.ToString().ToUpperInvariant());
            categoryLabel.AddToClassList("ai-node-cat");
            categoryLabel.AddToClassList("ai-f-mono");
            rootLabel = new Label("ROOT");
            rootLabel.AddToClassList("ai-root-label");
            rootLabel.AddToClassList("ai-f-mono");
            var spacer = new VisualElement();
            spacer.AddToClassList("ai-node-head-spacer");
            badge = new Label("!");
            badge.AddToClassList("ai-badge");
            dot = new VisualElement();
            dot.AddToClassList("ai-node-dot");
            head.Add(categoryLabel);
            head.Add(rootLabel);
            head.Add(spacer);
            head.Add(badge);
            head.Add(dot);
            nameLabel = new Label();
            nameLabel.AddToClassList("ai-node-name");
            subtitle = new Label();
            subtitle.AddToClassList("ai-node-subtitle");
            subtitle.AddToClassList("ai-f-mono");
            mainContainer.Add(head);
            mainContainer.Add(nameLabel);
            mainContainer.Add(subtitle);
            // Top-down layout: parent port centred on the card's top edge, child port on its bottom edge.
            inputContainer.RemoveFromHierarchy();
            outputContainer.RemoveFromHierarchy();
            inputContainer.AddToClassList("ai-port-row");
            inputContainer.AddToClassList("ai-port-row-in");
            outputContainer.AddToClassList("ai-port-row");
            outputContainer.AddToClassList("ai-port-row-out");
            Add(inputContainer);
            Add(outputContainer);
            abortChip = new Label { pickingMode = PickingMode.Ignore };
            abortChip.AddToClassList("ai-abort-chip");
            abortChip.AddToClassList("ai-f-mono");
            Add(abortChip);
            // Missing nodes are not movable: their position lives in data Unity will not let us edit, so a drag would snap back.
            if (IsMissing) capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable | Capabilities.Movable);
            Refresh(null, false, null);
        }

        public string NodeId { get; }

        public AINodeCategory Category { get; }

        public bool IsMissing => Category == AINodeCategory.Missing;

        public AIPort Input { get; }

        public AIPort Output { get; }

        public string NameText => nameLabel.text;

        public string CategoryText => categoryLabel.text;

        public string AbortChipText => abortChip.text;

        public bool AbortChipVisible => abortChip.style.display.value != DisplayStyle.None;

        public void Refresh(AINode node, bool isRoot, IReadOnlyList<string> issues)
        {
            if (node != null)
            {
                AINodeTypeInfo info = AINodeTypeCatalog.Describe(node.GetType());
                bool customTitle = node.Title != node.GetType().Name;
                string name = customTitle ? node.Title : info.Name;
                title = name;
                nameLabel.text = name;
                subtitle.text = customTitle ? info.Name : string.Empty;
                tooltip = info.Description;
                abortChip.text = AbortText(node);
            }
            bool hasSub = !string.IsNullOrEmpty(subtitle.text);
            subtitle.style.display = hasSub ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList("ai-no-sub", !hasSub);
            abortChip.style.display = string.IsNullOrEmpty(abortChip.text) ? DisplayStyle.None : DisplayStyle.Flex;
            EnableInClassList("ai-root", isRoot);
            rootLabel.style.display = isRoot ? DisplayStyle.Flex : DisplayStyle.None;
            bool hasIssues = issues != null && issues.Count > 0;
            badge.style.display = hasIssues ? DisplayStyle.Flex : DisplayStyle.None;
            badge.tooltip = hasIssues ? string.Join("\n", issues) : string.Empty;
        }

        /// <summary>Chip text for a condition's abort type, or empty when the node cannot abort.</summary>
        public static string AbortText(AINode node) => node is ConditionNode condition ? AbortText(condition.AbortType) : string.Empty;

        public static string AbortText(AbortType abortType)
        {
            switch (abortType)
            {
                case AbortType.Self: return "abort: Self";
                case AbortType.LowerPriority: return "abort: Lower";
                case AbortType.Both: return "abort: Both";
                default: return string.Empty;
            }
        }

        public void SetMissing(string className)
        {
            title = "Missing: " + className;
            nameLabel.text = title;
            tooltip = "This node's class was deleted or renamed without [MovedFrom]. Restore the class, or use Remove Missing Nodes in the toolbar menu.";
        }

        /// <summary>Debugger status class (ai-status-*), or null to clear.</summary>
        public void SetStatus(string cssClass)
        {
            if (statusClass == cssClass) return;
            if (!string.IsNullOrEmpty(statusClass)) RemoveFromClassList(statusClass);
            if (!string.IsNullOrEmpty(cssClass)) AddToClassList(cssClass);
            statusClass = cssClass;
        }

        /// <summary>Dims the status dot of a running node; the debugger alternates this to pulse it.</summary>
        public void SetPulse(bool dim)
        {
            EnableInClassList("ai-pulse-dim", dim && statusClass == RunningClass);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            AddToClassList("ai-selected");
            selectionChanged?.Invoke();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            RemoveFromClassList("ai-selected");
            selectionChanged?.Invoke();
        }
    }
}
