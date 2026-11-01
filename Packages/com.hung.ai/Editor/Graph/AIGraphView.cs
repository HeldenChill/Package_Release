using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>GraphView over one AIGraphAsset. Every edit goes through AIGraphEditing; the view only mirrors the asset.</summary>
    public sealed class AIGraphView : GraphView
    {
        const string ActiveEdgeClass = "ai-edge-active";
        static readonly Color ActiveEdgeColor = new Color(0.290f, 0.639f, 1f);
        readonly EditorWindow window;
        readonly AIEdgeConnectorListener listener;
        readonly AINodeSearchProvider searchProvider;
        readonly Dictionary<string, AINodeView> nodeViews = new Dictionary<string, AINodeView>();
        readonly MiniMap miniMap;
        readonly Label zoomLabel;
        AIGraphAsset asset;
        Port pendingPort;
        bool loading;
        bool selectionNotifyQueued;

        public AIGraphView(EditorWindow window)
        {
            this.window = window;
            AddToClassList("ai-graph-view");
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            listener = new AIEdgeConnectorListener(this);
            searchProvider = ScriptableObject.CreateInstance<AINodeSearchProvider>();
            searchProvider.hideFlags = HideFlags.HideAndDontSave;
            searchProvider.Init(this);
            nodeCreationRequest = context => OpenSearch(context.screenMousePosition, null);
            graphViewChanged = OnGraphViewChanged;
            serializeGraphElements = OnSerializeGraphElements;
            canPasteSerializedData = AIGraphClipboard.CanPaste;
            unserializeAndPaste = OnUnserializeAndPaste;
            elementsAddedToGroup = OnElementsAddedToGroup;
            elementsRemovedFromGroup = OnElementsRemovedFromGroup;
            groupTitleChanged = OnGroupTitleChanged;
            miniMap = new MiniMap { anchored = true };
            miniMap.AddToClassList("ai-minimap");
            Add(miniMap);
            // The zoom label below replaces the minimap's own "MiniMap 0.66x" caption.
            Label miniMapCaption = miniMap.Q<Label>();
            if (miniMapCaption != null) miniMapCaption.style.display = DisplayStyle.None;
            zoomLabel = new Label("100%") { pickingMode = PickingMode.Ignore };
            zoomLabel.AddToClassList("ai-zoom");
            zoomLabel.AddToClassList("ai-f-mono");
            Add(zoomLabel);
            Add(BuildLegend());
            viewTransformChanged = _ => UpdateZoomLabel();
            RegisterCallback<GeometryChangedEvent>(_ => PlaceMiniMap());
        }

        public string ZoomText => zoomLabel.text;

        /// <summary>Keeps the minimap anchored 10px from the bottom-right corner, as in the mockup.</summary>
        void PlaceMiniMap()
        {
            Rect area = layout;
            miniMap.SetPosition(new Rect(area.width - 170f, area.height - 110f, 160f, 100f));
        }

        void UpdateZoomLabel()
        {
            zoomLabel.text = Mathf.RoundToInt(viewTransform.scale.x * 100f) + "%";
        }

        static VisualElement BuildLegend()
        {
            var legend = new VisualElement { pickingMode = PickingMode.Ignore };
            legend.AddToClassList("ai-legend");
            AddLegendItem(legend, "Running", "ai-legend-run");
            AddLegendItem(legend, "Success", "ai-legend-ok");
            AddLegendItem(legend, "Failure", "ai-legend-fail");
            AddLegendItem(legend, "Aborted", "ai-legend-abort");
            return legend;
        }

        static void AddLegendItem(VisualElement legend, string text, string dotClass)
        {
            var item = new VisualElement { pickingMode = PickingMode.Ignore };
            item.AddToClassList("ai-legend-item");
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("ai-legend-dot");
            dot.AddToClassList(dotClass);
            item.Add(dot);
            item.Add(new Label(text) { pickingMode = PickingMode.Ignore });
            legend.Add(item);
        }

        /// <summary>Raised with the selected node id, or null when no node is selected.</summary>
        public event Action<string> NodeSelected;

        /// <summary>Raised after any structural edit (create, delete, link, move, group, note).</summary>
        public event Action GraphChanged;

        public AIGraphAsset Asset => asset;

        public IReadOnlyDictionary<string, AINodeView> NodeViews => nodeViews;

        public bool MiniMapVisible
        {
            get => miniMap.style.display.value != DisplayStyle.None;
            set => miniMap.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            if (searchProvider != null) UnityEngine.Object.DestroyImmediate(searchProvider);
        }

        /// <summary>Rebuilds every element from the asset. Called on open and after undo/redo.</summary>
        public void Load(AIGraphAsset graph)
        {
            loading = true;
            try
            {
                var old = new List<GraphElement>();
                graphElements.ForEach(e =>
                {
                    if (!(e is MiniMap)) old.Add(e);
                });
                DeleteElements(old);
                nodeViews.Clear();
                pendingPort = null;
                asset = graph;
                if (asset == null) return;
                foreach (AINode node in asset.Nodes)
                    if (node != null) AddNodeView(node.Id, AINodeTypeCatalog.CategoryOf(node.GetType()), node.EditorPosition);
                foreach (MissingNodeInfo missing in AIMissingNodes.Read(asset))
                    if (!nodeViews.ContainsKey(missing.NodeId)) AddNodeView(missing.NodeId, AINodeCategory.Missing, missing.Position).SetMissing(missing.ClassName);
                foreach (AINode node in asset.Nodes)
                {
                    if (node == null || !nodeViews.TryGetValue(node.Id, out AINodeView parent)) continue;
                    IReadOnlyList<string> ids = AIGraphAsset.ChildIdsOf(node);
                    for (int i = 0; i < ids.Count; i++)
                        if (nodeViews.TryGetValue(ids[i], out AINodeView child)) LinkViews(parent, child);
                }
                foreach (AIGraphGroup group in asset.Groups) AddGroupView(group);
                foreach (AIGraphNote note in asset.Notes) AddElement(new AINoteView(note, OnNoteChanged));
            }
            finally
            {
                loading = false;
            }
            RefreshNodes();
        }

        /// <summary>Re-reads titles, root marker and validation badges for every node view.</summary>
        public void RefreshNodes()
        {
            if (asset == null) return;
            Dictionary<string, List<string>> issues = AIGraphValidator.ValidateAll(asset);
            foreach (KeyValuePair<string, AINodeView> pair in nodeViews)
            {
                issues.TryGetValue(pair.Key, out List<string> nodeIssues);
                pair.Value.Refresh(asset.GetNode(pair.Key), pair.Key == asset.RootNodeId, nodeIssues);
            }
        }

        public void OpenSearch(Vector2 screenPosition, Port fromPort)
        {
            if (asset == null) return;
            pendingPort = fromPort;
            SearchWindow.Open(new SearchWindowContext(screenPosition), searchProvider);
        }

        public void OpenSearchFromPort(Port fromPort, Vector2 panelPosition)
        {
            OpenSearch(window.position.position + panelPosition, fromPort);
        }

        public Vector2 ScreenToGraph(Vector2 screenPosition)
        {
            Vector2 windowPosition = screenPosition - window.position.position;
            Vector2 world = window.rootVisualElement.ChangeCoordinatesTo(window.rootVisualElement.parent, windowPosition);
            return contentViewContainer.WorldToLocal(world);
        }

        public void CreateNodeAt(Type type, Vector2 graphPosition)
        {
            AINode node = AIGraphEditing.CreateNode(asset, type, graphPosition);
            if (node == null) return;
            AINodeView view = AddNodeView(node.Id, AINodeTypeCatalog.CategoryOf(type), graphPosition);
            if (pendingPort != null && pendingPort.node is AINodeView other)
            {
                if (pendingPort.direction == Direction.Output) TryLink(other, view);
                else TryLink(view, other);
            }
            pendingPort = null;
            ClearSelection();
            AddToSelection(view);
            RefreshNodes();
            GraphChanged?.Invoke();
        }

        public void CreateGroupFromSelection(Vector2 graphPosition)
        {
            if (asset == null) return;
            List<string> ids = SelectedNodeIds();
            loading = true;
            try
            {
                AIGraphGroup group = AIGraphEditing.CreateGroup(asset, "Group", graphPosition, ids);
                AddGroupView(group);
            }
            finally
            {
                loading = false;
            }
            GraphChanged?.Invoke();
        }

        public void CreateNote(Vector2 graphPosition)
        {
            if (asset == null) return;
            AIGraphNote note = AIGraphEditing.CreateNote(asset, new Rect(graphPosition, new Vector2(200f, 160f)));
            AddElement(new AINoteView(note, OnNoteChanged));
            GraphChanged?.Invoke();
        }

        /// <summary>Lines selected nodes up on their left edges (true) or top edges (false).</summary>
        public void AlignSelection(bool leftEdges)
        {
            if (asset == null) return;
            var views = new List<AINodeView>();
            foreach (ISelectable selectable in selection)
                if (selectable is AINodeView view && !view.IsMissing) views.Add(view);
            if (views.Count < 2) return;
            float target = float.MaxValue;
            foreach (AINodeView view in views)
            {
                Rect rect = view.GetPosition();
                target = Mathf.Min(target, leftEdges ? rect.x : rect.y);
            }
            var positions = new Dictionary<string, Vector2>();
            foreach (AINodeView view in views)
            {
                Rect rect = view.GetPosition();
                Vector2 position = leftEdges ? new Vector2(target, rect.y) : new Vector2(rect.x, target);
                view.SetPosition(new Rect(position, rect.size));
                positions[view.NodeId] = position;
            }
            AIGraphEditing.MoveNodes(asset, positions);
            GraphChanged?.Invoke();
        }

        public void ClearDebug()
        {
            foreach (AINodeView view in nodeViews.Values) view.SetStatus(null);
            edges.ForEach(e => SetEdgeActive(e, false));
        }

        public void HighlightActiveEdges(HashSet<string> activeIds)
        {
            edges.ForEach(e =>
            {
                bool active = e.output?.node is AINodeView parent && e.input?.node is AINodeView child
                    && activeIds.Contains(parent.NodeId) && activeIds.Contains(child.NodeId);
                SetEdgeActive(e, active);
            });
        }

        /// <summary>The USS class sets the width. Connected edges take their color from the ports, so the color is set here (re-applied every debugger poll).</summary>
        static void SetEdgeActive(Edge edge, bool active)
        {
            edge.EnableInClassList(ActiveEdgeClass, active);
            if (edge.edgeControl == null || edge.selected || edge.input == null || edge.output == null) return;
            edge.edgeControl.inputColor = active ? ActiveEdgeColor : edge.input.portColor;
            edge.edgeControl.outputColor = active ? ActiveEdgeColor : edge.output.portColor;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();
            if (asset == null || !(startPort.node is AINodeView start) || start.IsMissing) return result;
            ports.ForEach(port =>
            {
                if (port.direction == startPort.direction || !(port.node is AINodeView other) || other == start || other.IsMissing) return;
                string parentId = startPort.direction == Direction.Output ? start.NodeId : other.NodeId;
                string childId = startPort.direction == Direction.Output ? other.NodeId : start.NodeId;
                if (AIGraphEditing.CanConnect(asset, parentId, childId)) result.Add(port);
            });
            return result;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (asset != null)
            {
                Vector2 graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
                Vector2 screenPosition = window.position.position + evt.mousePosition;
                AINodeView clicked = (evt.target as VisualElement)?.GetFirstOfType<AINodeView>();
                if (clicked != null && !clicked.IsMissing)
                {
                    string clickedId = clicked.NodeId;
                    evt.menu.AppendAction("Set as Root", _ => SetRoot(clickedId), clickedId == asset.RootNodeId ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                }
                evt.menu.AppendAction("Create Node", _ => OpenSearch(screenPosition, null));
                evt.menu.AppendAction("Create Sticky Note", _ => CreateNote(graphPosition));
                DropdownMenuAction.Status selectionStatus = SelectedNodeIds().Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                evt.menu.AppendAction("Group Selection", _ => CreateGroupFromSelection(graphPosition), selectionStatus);
                evt.menu.AppendAction("Align/Left Edges", _ => AlignSelection(true), selectionStatus);
                evt.menu.AppendAction("Align/Top Edges", _ => AlignSelection(false), selectionStatus);
                evt.menu.AppendSeparator();
            }
            base.BuildContextualMenu(evt);
        }

        AINodeView AddNodeView(string nodeId, AINodeCategory category, Vector2 position)
        {
            var view = new AINodeView(nodeId, category, listener, QueueSelectionNotify);
            view.SetPosition(new Rect(position, Vector2.zero));
            AddElement(view);
            nodeViews[nodeId] = view;
            return view;
        }

        void AddGroupView(AIGraphGroup group)
        {
            var view = new AIGroupView(group.Id, group.Title);
            view.SetPosition(new Rect(group.Position, Vector2.zero));
            AddElement(view);
            foreach (string id in group.NodeIds)
                if (nodeViews.TryGetValue(id, out AINodeView node)) view.AddElement(node);
        }

        void LinkViews(AINodeView parent, AINodeView child)
        {
            if (parent.Output == null) return;
            Edge edge = parent.Output.ConnectTo<AIEdge>(child.Input);
            AddElement(edge);
        }

        void TryLink(AINodeView parent, AINodeView child)
        {
            if (parent.Output == null || !AIGraphEditing.Connect(asset, parent.NodeId, child.NodeId)) return;
            RemoveEdges(child.Input);
            if (parent.Output.capacity == Port.Capacity.Single) RemoveEdges(parent.Output);
            LinkViews(parent, child);
        }

        void RemoveEdges(Port port)
        {
            var stale = new List<Edge>(port.connections);
            foreach (Edge edge in stale)
            {
                edge.input?.Disconnect(edge);
                edge.output?.Disconnect(edge);
                RemoveElement(edge);
            }
        }

        void SetRoot(string nodeId)
        {
            bool hadParent = AIGraphEditing.ParentIdOf(asset, nodeId) != null;
            AIGraphEditing.SetRoot(asset, nodeId);
            if (hadParent && nodeViews.TryGetValue(nodeId, out AINodeView view)) RemoveEdges(view.Input);
            RefreshNodes();
            GraphChanged?.Invoke();
        }

        List<string> SelectedNodeIds()
        {
            var ids = new List<string>();
            foreach (ISelectable selectable in selection)
                if (selectable is AINodeView view && !view.IsMissing) ids.Add(view.NodeId);
            return ids;
        }

        void QueueSelectionNotify()
        {
            if (selectionNotifyQueued) return;
            selectionNotifyQueued = true;
            schedule.Execute(() =>
            {
                selectionNotifyQueued = false;
                List<string> ids = SelectedNodeIds();
                NodeSelected?.Invoke(ids.Count > 0 ? ids[0] : null);
            });
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (loading || asset == null) return change;
            bool changed = false;
            if (change.elementsToRemove != null && change.elementsToRemove.Count > 0)
            {
                var removedNodes = new List<string>();
                foreach (GraphElement element in change.elementsToRemove)
                {
                    switch (element)
                    {
                        case Edge edge when edge.output?.node is AINodeView parent && edge.input?.node is AINodeView child:
                            AIGraphEditing.Disconnect(asset, parent.NodeId, child.NodeId);
                            break;
                        case AINodeView node:
                            removedNodes.Add(node.NodeId);
                            break;
                        case AIGroupView group:
                            AIGraphEditing.RemoveGroup(asset, group.GroupId);
                            break;
                        case AINoteView note:
                            AIGraphEditing.RemoveNote(asset, note.NoteId);
                            break;
                    }
                }
                if (removedNodes.Count > 0)
                {
                    AIGraphEditing.DeleteNodes(asset, removedNodes);
                    foreach (string id in removedNodes) nodeViews.Remove(id);
                }
                changed = true;
            }
            if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
            {
                for (int i = change.edgesToCreate.Count - 1; i >= 0; i--)
                {
                    Edge edge = change.edgesToCreate[i];
                    bool linked = edge.output?.node is AINodeView parent && edge.input?.node is AINodeView child
                        && AIGraphEditing.Connect(asset, parent.NodeId, child.NodeId);
                    if (!linked) change.edgesToCreate.RemoveAt(i);
                }
                changed = true;
            }
            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                var positions = new Dictionary<string, Vector2>();
                foreach (GraphElement element in change.movedElements)
                {
                    if (element is AINodeView node)
                    {
                        positions[node.NodeId] = node.GetPosition().position;
                    }
                    else if (element is AINoteView note)
                    {
                        AIGraphEditing.UpdateNote(asset, note.NoteId, note.title, note.contents, note.GetPosition());
                    }
                    else if (element is AIGroupView group)
                    {
                        AIGraphEditing.SetGroupPosition(asset, group.GroupId, group.GetPosition().position);
                        foreach (GraphElement contained in group.containedElements)
                            if (contained is AINodeView member) positions[member.NodeId] = member.GetPosition().position;
                    }
                }
                if (positions.Count > 0) AIGraphEditing.MoveNodes(asset, positions);
                changed = true;
            }
            if (changed)
            {
                RefreshNodes();
                GraphChanged?.Invoke();
            }
            return change;
        }

        string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var ids = new List<string>();
            foreach (GraphElement element in elements)
                if (element is AINodeView view && !view.IsMissing) ids.Add(view.NodeId);
            return asset != null ? AIGraphClipboard.Serialize(asset, ids) : string.Empty;
        }

        void OnUnserializeAndPaste(string operationName, string data)
        {
            if (asset == null) return;
            List<AINode> pasted = AIGraphClipboard.Paste(asset, data, new Vector2(40f, 40f));
            if (pasted.Count == 0) return;
            ClearSelection();
            foreach (AINode node in pasted)
                AddToSelection(AddNodeView(node.Id, AINodeTypeCatalog.CategoryOf(node.GetType()), node.EditorPosition));
            foreach (AINode node in pasted)
            {
                IReadOnlyList<string> ids = AIGraphAsset.ChildIdsOf(node);
                for (int i = 0; i < ids.Count; i++)
                    if (nodeViews.TryGetValue(ids[i], out AINodeView child)) LinkViews(nodeViews[node.Id], child);
            }
            RefreshNodes();
            GraphChanged?.Invoke();
        }

        void OnElementsAddedToGroup(Group group, IEnumerable<GraphElement> elements)
        {
            if (loading || asset == null || !(group is AIGroupView view)) return;
            AIGraphEditing.AddToGroup(asset, view.GroupId, NodeIdsOf(elements));
        }

        void OnElementsRemovedFromGroup(Group group, IEnumerable<GraphElement> elements)
        {
            if (loading || asset == null || !(group is AIGroupView view)) return;
            AIGraphEditing.RemoveFromGroup(asset, view.GroupId, NodeIdsOf(elements));
        }

        void OnGroupTitleChanged(Group group, string newTitle)
        {
            if (loading || asset == null || !(group is AIGroupView view)) return;
            AIGraphEditing.SetGroupTitle(asset, view.GroupId, newTitle);
        }

        void OnNoteChanged(AINoteView view)
        {
            if (loading || asset == null) return;
            AIGraphEditing.UpdateNote(asset, view.NoteId, view.title, view.contents, view.GetPosition());
        }

        static List<string> NodeIdsOf(IEnumerable<GraphElement> elements)
        {
            var ids = new List<string>();
            foreach (GraphElement element in elements)
                if (element is AINodeView view) ids.Add(view.NodeId);
            return ids;
        }
    }
}
