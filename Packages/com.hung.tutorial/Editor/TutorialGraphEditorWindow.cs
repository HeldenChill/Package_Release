using System.Collections.Generic;
using System.Text;
using Hung.Base;
using UnityEditor;
using UnityEngine;

namespace Hung.Tutorial.EditorTools
{
    /// <summary>
    /// IMGUI visual graph editor for TutorialGraphSO.
    /// - Saved free node positions for hand-made tutorial flow diagrams.
    /// - Auto layout for TutorialData-style branch/advance-step flows.
    /// - Drag node body to move, drag bottom port to connect, click highlighted edge to delete.
    /// </summary>
    public class TutorialGraphEditorWindow : EditorWindow
    {
        private const float BaseNodeW = 220f;
        private const float BaseNodeH = 118f;
        private const float BaseTitleH = 28f;
        private const float BaseMargin = 72f;
        private const float BaseRankGapY = 178f;
        private const float BaseSiblingGapX = 290f;
        private const float BasePortR = 6f;
        private const float BaseGrid = 24f;
        private const float EdgeClickDist = 8f;
        private const float SplitterW = 5f;
        private const float InspectorMinW = 240f;
        private const float InspectorMaxW = 640f;

        private float zoom = 1f;
        private float NodeW => BaseNodeW * zoom;
        private float NodeH => BaseNodeH * zoom;
        private float TitleH => BaseTitleH * zoom;
        private float PortR => BasePortR * zoom;
        private float Margin => BaseMargin * zoom;

        private TutorialGraphSO graph;
        private SerializedObject so;

        private readonly Dictionary<int, Rect> _layout = new Dictionary<int, Rect>();
        private readonly HashSet<EdgeKey> _backEdges = new HashSet<EdgeKey>();
        private readonly List<EdgeDrawInfo> _edgeCache = new List<EdgeDrawInfo>();
        private readonly Dictionary<int, Vector2> _previewPositions = new Dictionary<int, Vector2>();
        private readonly Vector3[] _sampleBuffer = new Vector3[25];
        private bool _usingPreviewLayout;

        private float _contentW;
        private float _contentH;
        private Rect _canvasAreaRect;
        private Vector2 canvasScroll;

        private int selectedId = -1;
        private int draggingId = -1;
        private Vector2 dragMouseOffset;
        private bool dragStarted;
        private bool panning;
        private bool isLinking;
        private int linkFromId = -1;
        private int hoverEdgeFrom = -1;
        private int hoverEdgeTo = -1;

        private bool showLegend = true;
        private bool showInspector = true;
        private bool showMinimap = true;
        private bool showGrid = true;
        private bool showNodeDetails = true;
        private bool showEdgeLabels = true;
        private bool snapToGrid = true;
        private bool followRuntime = true;
        private bool autoFocusRuntime = true;
        private bool _draggingSplitter;
        private float _inspectorW = 380f;
        private Vector2 _inspectorScroll;

        private bool _rtActive;
        private int _rtCurrentId = -1;
        private int _rtCurrentLevel = -1;
        private int _rtCurrentStep = -1;
        private int _rtLastFromId = -1;
        private int _rtLastToId = -1;
        private int _rtPreviousCurrentId = -1;
        private int _rtLastFocusedId = -1;
        private bool _pendingRuntimeFocus;
        private readonly HashSet<int> _rtVisitedIds = new HashSet<int>();

        private GUIStyle _titleStyle;
        private GUIStyle _miniCenterStyle;
        private GUIStyle _badgeStyle;

        [MenuItem("Tools/Tutorial/Graph Editor")]
        public static void OpenMenu() => Open(null);

        public static void Open(TutorialGraphSO target)
        {
            var win = GetWindow<TutorialGraphEditorWindow>("Tutorial Graph");
            win.minSize = new Vector2(980, 560);
            if (target != null) win.SetGraph(target);
            win.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            if (graph == null && Selection.activeObject is TutorialGraphSO g) SetGraph(g);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is TutorialGraphSO g && g != graph)
            {
                SetGraph(g);
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            if (followRuntime && Application.isPlaying) Repaint();
        }

        private void SetGraph(TutorialGraphSO g)
        {
            graph = g;
            so = graph != null ? new SerializedObject(graph) : null;
            selectedId = -1;
            draggingId = -1;
            isLinking = false;
            linkFromId = -1;
            hoverEdgeFrom = hoverEdgeTo = -1;
            _rtVisitedIds.Clear();
            _rtLastFocusedId = -1;
            _pendingRuntimeFocus = false;
        }

        private void RefreshSerialized() => so = graph != null ? new SerializedObject(graph) : null;

        private void RefreshRuntimeState()
        {
            int previousCurrentId = _rtCurrentId;

            _rtActive = followRuntime
                && Application.isPlaying
                && Locator.Tutorial != null
                && Locator.Tutorial.IsRunning;

            _rtCurrentId = -1;
            _rtCurrentLevel = -1;
            _rtCurrentStep = -1;

            if (!_rtActive || graph == null)
            {
                _rtVisitedIds.Clear();
                _rtPreviousCurrentId = -1;
                _rtLastFromId = -1;
                _rtLastToId = -1;
                _rtLastFocusedId = -1;
                _pendingRuntimeFocus = false;
                return;
            }

            _rtCurrentLevel = Locator.Tutorial.CurrentStepLevel;
            _rtCurrentStep = Locator.Tutorial.CurrentStep;
            _rtCurrentId = FindRuntimeNodeId(_rtCurrentLevel, _rtCurrentStep);

            if (_rtCurrentId < 0) return;

            _rtVisitedIds.Add(_rtCurrentId);

            if (previousCurrentId >= 0 && previousCurrentId != _rtCurrentId)
            {
                _rtPreviousCurrentId = previousCurrentId;
                if (TryResolveRuntimeTransition(previousCurrentId, _rtCurrentId, out int fromId, out int toId))
                {
                    _rtLastFromId = fromId;
                    _rtLastToId = toId;
                }
                _pendingRuntimeFocus = true;
            }
            else if (_rtLastFocusedId != _rtCurrentId)
            {
                _pendingRuntimeFocus = true;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            RefreshRuntimeState();
            DrawToolbar();

            if (graph == null)
            {
                EditorGUILayout.HelpBox("Select or assign a TutorialGraphSO asset to edit the tutorial flow graph.", MessageType.Info);
                return;
            }

            var full = new Rect(0, 22, position.width, position.height - 22);
            float legendW = showLegend ? 160f : 0f;
            float inspectorW = showInspector ? _inspectorW : 0f;
            float splitterW = showInspector ? SplitterW : 0f;
            var legendRect = new Rect(full.x, full.y, legendW, full.height);
            var splitterRect = new Rect(full.xMax - inspectorW - splitterW, full.y, splitterW, full.height);
            var canvasRect = new Rect(full.x + legendW, full.y, splitterRect.x - full.x - legendW, full.height);
            var inspectorRect = new Rect(splitterRect.xMax, full.y, inspectorW, full.height);

            if (showInspector) HandleSplitter(splitterRect);
            if (showLegend) DrawLegendColumn(legendRect);
            DrawCanvas(canvasRect);
            if (showInspector) DrawInspector(inspectorRect);
            if (showLegend && showMinimap) DrawMinimap(legendRect);

            if (isLinking || hoverEdgeFrom >= 0 || draggingId >= 0 || _rtActive) Repaint();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            var picked = (TutorialGraphSO)EditorGUILayout.ObjectField(graph, typeof(TutorialGraphSO), false, GUILayout.Width(230));
            if (EditorGUI.EndChangeCheck()) SetGraph(picked);

            using (new EditorGUI.DisabledScope(graph == null))
            {
                if (GUILayout.Button("Add Root", EditorStyles.toolbarButton, GUILayout.Width(72))) AddRoot();
                if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(86))) AutoLayout();
                if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(38))) FitToGraph();
                if (GUILayout.Button("Sync", EditorStyles.toolbarButton, GUILayout.Width(46))) SyncAllFromActions();
                if (GUILayout.Button("RT Ids", EditorStyles.toolbarButton, GUILayout.Width(52))) BackfillRuntimeIdsFromCurrentOrder();
                if (GUILayout.Button("Clean", EditorStyles.toolbarButton, GUILayout.Width(48))) CleanMissingEdges();
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(46))) SaveGraph();

                GUILayout.Space(8);
                GUILayout.Label("Zoom", EditorStyles.miniLabel, GUILayout.Width(36));
                zoom = GUILayout.HorizontalSlider(zoom, 0.45f, 1.6f, GUILayout.Width(100));
                if (GUILayout.Button("1x", EditorStyles.toolbarButton, GUILayout.Width(30))) zoom = 1f;

                GUILayout.Space(6);
                showGrid = GUILayout.Toggle(showGrid, "Grid", EditorStyles.toolbarButton, GUILayout.Width(46));
                snapToGrid = GUILayout.Toggle(snapToGrid, "Snap", EditorStyles.toolbarButton, GUILayout.Width(50));
                showNodeDetails = GUILayout.Toggle(showNodeDetails, "Details", EditorStyles.toolbarButton, GUILayout.Width(58));
                showEdgeLabels = GUILayout.Toggle(showEdgeLabels, "Labels", EditorStyles.toolbarButton, GUILayout.Width(54));
                showLegend = GUILayout.Toggle(showLegend, "Legend", EditorStyles.toolbarButton, GUILayout.Width(58));
                showMinimap = GUILayout.Toggle(showMinimap, "Map", EditorStyles.toolbarButton, GUILayout.Width(42));
                showInspector = GUILayout.Toggle(showInspector, "Inspector", EditorStyles.toolbarButton, GUILayout.Width(72));
            }

            GUILayout.Space(8);
            followRuntime = GUILayout.Toggle(followRuntime, "Follow Run", EditorStyles.toolbarButton, GUILayout.Width(78));
            using (new EditorGUI.DisabledScope(!followRuntime))
            {
                autoFocusRuntime = GUILayout.Toggle(autoFocusRuntime, "Auto Focus", EditorStyles.toolbarButton, GUILayout.Width(82));
            }
            if (_rtActive)
            {
                var prev = GUI.color;
                GUI.color = _rtCurrentId >= 0 ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.72f, 0.32f);
                string runtimeText = _rtCurrentId >= 0
                    ? $"● L{_rtCurrentLevel} S{_rtCurrentStep}"
                    : $"● L{_rtCurrentLevel} S{_rtCurrentStep} ?";
                GUILayout.Label(runtimeText, EditorStyles.miniBoldLabel, GUILayout.Width(92));
                GUI.color = prev;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag node = move freely · drag bottom dot = connect · click edge = delete · MMB/Alt-drag = pan", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCanvas(Rect area)
        {
            _canvasAreaRect = area;
            GUI.Box(area, GUIContent.none);

            HandlePan(area);
            HandleWheelZoom(area);
            BuildLayout(area);
            MaybeAutoFocusRuntimeNode();

            canvasScroll = GUI.BeginScrollView(area, canvasScroll, new Rect(0, 0, _contentW, _contentH));

            if (showGrid) DrawGrid();
            DrawRankGuides();
            BuildEdgeCache();
            UpdateHoverEdge();
            DrawEdges();
            if (isLinking) DrawLinkingLine();

            for (int i = 0; i < graph.nodes.Count; i++)
                DrawNode(graph.nodes[i]);

            HandleCanvasEvents();
            GUI.EndScrollView();

            HandleKeyboardShortcuts();
        }

        private void BuildLayout(Rect area)
        {
            _layout.Clear();
            ComputeBackEdges();
            _usingPreviewLayout = !HasAnyManualPosition();
            _previewPositions.Clear();
            if (_usingPreviewLayout) BuildPreviewPositions(area);

            _contentW = Mathf.Max(area.width, 900f);
            _contentH = Mathf.Max(area.height, 600f);

            foreach (var node in graph.nodes)
            {
                Vector2 graphPos = GetGraphPositionForLayout(node);
                var rect = new Rect(graphPos.x * zoom, graphPos.y * zoom, NodeW, NodeH);
                _layout[node.id] = rect;
                _contentW = Mathf.Max(_contentW, rect.xMax + Margin);
                _contentH = Mathf.Max(_contentH, rect.yMax + Margin);
            }
        }

        private Vector2 GetGraphPosition(TutorialGraphSO.TutorialStepNode node)
        {
            if (node.useManualPosition) return node.editorPosition;
            return new Vector2(BaseMargin + node.slot * BaseSiblingGapX, BaseMargin + node.layer * BaseRankGapY);
        }

        private Vector2 GetGraphPositionForLayout(TutorialGraphSO.TutorialStepNode node)
        {
            if (_usingPreviewLayout && _previewPositions.TryGetValue(node.id, out var preview)) return preview;
            return GetGraphPosition(node);
        }

        private bool HasAnyManualPosition()
        {
            if (graph == null) return false;
            foreach (var node in graph.nodes)
                if (node.useManualPosition) return true;
            return false;
        }

        private void BuildPreviewPositions(Rect area)
        {
            if (graph == null || graph.nodes.Count == 0) return;

            var rankMap = CalculateRanksIgnoringBackEdges();
            var byRank = new Dictionary<int, List<TutorialGraphSO.TutorialStepNode>>();
            int maxRank = 0;
            foreach (var node in graph.nodes)
            {
                int rank = rankMap.ContainsKey(node.id) ? rankMap[node.id] : node.layer;
                if (!byRank.ContainsKey(rank)) byRank[rank] = new List<TutorialGraphSO.TutorialStepNode>();
                byRank[rank].Add(node);
                maxRank = Mathf.Max(maxRank, rank);
            }

            int maxCount = 1;
            foreach (var kvp in byRank)
            {
                maxCount = Mathf.Max(maxCount, kvp.Value.Count);
                kvp.Value.Sort((a, b) => a.slot != b.slot ? a.slot.CompareTo(b.slot) : a.id.CompareTo(b.id));
            }

            float visibleGraphW = Mathf.Max(900f, area.width > 1f ? area.width / zoom : 900f);
            float contentGraphW = Mathf.Max(visibleGraphW, BaseMargin * 2f + BaseNodeW + (maxCount - 1) * BaseSiblingGapX);
            float centerX = contentGraphW * 0.5f;

            for (int rank = 0; rank <= maxRank; rank++)
            {
                if (!byRank.ContainsKey(rank)) continue;
                var row = byRank[rank];
                for (int i = 0; i < row.Count; i++)
                {
                    _previewPositions[row[i].id] = new Vector2(
                        centerX + (i - (row.Count - 1) * 0.5f) * BaseSiblingGapX - BaseNodeW * 0.5f,
                        BaseMargin + rank * BaseRankGapY);
                }
            }
        }

        private void EnsureManualLayoutBeforeMutation()
        {
            if (graph == null || graph.nodes.Count == 0 || HasAnyManualPosition()) return;
            if (_layout.Count == 0) BuildLayout(_canvasAreaRect.width > 1f ? _canvasAreaRect : new Rect(0, 0, position.width, position.height));

            foreach (var node in graph.nodes)
            {
                Rect rect;
                if (!_layout.TryGetValue(node.id, out rect)) continue;
                node.useManualPosition = true;
                node.editorPosition = rect.position / zoom;
                UpdateLogicalOrderFromPosition(node);
            }
        }

        private void DrawGrid()
        {
            float step = BaseGrid * zoom;
            if (step < 8f) return;

            Color minor = new Color(1f, 1f, 1f, 0.055f);
            Color major = new Color(1f, 1f, 1f, 0.095f);

            for (float x = 0; x < _contentW; x += step)
            {
                Handles.color = Mathf.RoundToInt(x / step) % 5 == 0 ? major : minor;
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, _contentH));
            }

            for (float y = 0; y < _contentH; y += step)
            {
                Handles.color = Mathf.RoundToInt(y / step) % 5 == 0 ? major : minor;
                Handles.DrawLine(new Vector3(0, y), new Vector3(_contentW, y));
            }
        }

        private void DrawRankGuides()
        {
            int maxLayer = graph.LayerCount;
            for (int i = 0; i <= maxLayer; i++)
            {
                float y = (BaseMargin + i * BaseRankGapY - 14f) * zoom;
                EditorGUI.DrawRect(new Rect(0, y, _contentW, 1f), new Color(1f, 1f, 1f, 0.10f));
                GUI.Label(new Rect(8, y + 3f, 92, 18), $"Step rank {i}", EditorStyles.miniLabel);
            }
        }

        private Rect NodeRect(TutorialGraphSO.TutorialStepNode node)
        {
            return _layout.TryGetValue(node.id, out var rect) ? rect : new Rect(Margin, Margin, NodeW, NodeH);
        }

        private static Vector3 TopPort(Rect r) => new Vector3(r.center.x, r.yMin);
        private static Vector3 BottomPort(Rect r) => new Vector3(r.center.x, r.yMax);
        private static Vector3 LeftPort(Rect r) => new Vector3(r.xMin, r.center.y);
        private static Vector3 RightPort(Rect r) => new Vector3(r.xMax, r.center.y);

        private void BuildEdgeCache()
        {
            _edgeCache.Clear();
            foreach (var from in graph.nodes)
            {
                for (int i = 0; i < from.nextIds.Count; i++)
                {
                    var to = graph.GetNode(from.nextIds[i]);
                    if (to == null) continue;
                    var edge = CreateEdgeDrawInfo(from, to, i);
                    _edgeCache.Add(edge);
                }
            }
        }

        private EdgeDrawInfo CreateEdgeDrawInfo(TutorialGraphSO.TutorialStepNode from, TutorialGraphSO.TutorialStepNode to, int childIndex)
        {
            Rect aRect = NodeRect(from);
            Rect bRect = NodeRect(to);
            bool back = _backEdges.Contains(new EdgeKey(from.id, to.id)) || bRect.center.y <= aRect.center.y - NodeH * 0.25f;

            Vector3 p0;
            Vector3 p1;
            Vector3 c0;
            Vector3 c1;

            if (back)
            {
                bool routeLeft = aRect.center.x >= bRect.center.x;
                p0 = routeLeft ? LeftPort(aRect) : RightPort(aRect);
                p1 = routeLeft ? LeftPort(bRect) : RightPort(bRect);
                float offset = (110f + childIndex * 22f) * zoom;
                c0 = p0 + new Vector3(routeLeft ? -offset : offset, 0f, 0f);
                c1 = p1 + new Vector3(routeLeft ? -offset : offset, 0f, 0f);
            }
            else
            {
                p0 = BottomPort(aRect);
                p1 = TopPort(bRect);
                float dy = Mathf.Max(64f * zoom, Mathf.Abs(p1.y - p0.y) * 0.42f);
                float dxNudge = Mathf.Clamp((p1.x - p0.x) * 0.12f, -42f * zoom, 42f * zoom);
                c0 = p0 + new Vector3(dxNudge, dy, 0f);
                c1 = p1 - new Vector3(dxNudge, dy, 0f);
            }

            return new EdgeDrawInfo
            {
                fromId = from.id,
                toId = to.id,
                childIndex = childIndex,
                p0 = p0,
                p1 = p1,
                c0 = c0,
                c1 = c1,
                label = BuildEdgeLabel(from, to, childIndex),
                isBackEdge = back
            };
        }

        private string BuildEdgeLabel(TutorialGraphSO.TutorialStepNode from, TutorialGraphSO.TutorialStepNode to, int childIndex)
        {
            if (from.actionType == TUTORIAL_ACTION.BRANCH && from.action != null)
            {
                if (from.action.Conditions != null && childIndex >= 0 && childIndex < from.action.Conditions.Count)
                {
                    string branchCondition = from.action.Conditions[childIndex];
                    if (!string.IsNullOrEmpty(branchCondition)) return branchCondition;
                }

                if (!string.IsNullOrEmpty(to.conditionKey)) return to.conditionKey;
                return $"branch {childIndex + 1}";
            }

            if (!string.IsNullOrEmpty(to.conditionKey)) return to.conditionKey;
            return string.Empty;
        }

        private void UpdateHoverEdge()
        {
            hoverEdgeFrom = -1;
            hoverEdgeTo = -1;
            if (draggingId >= 0 || isLinking) return;

            Event e = Event.current;
            if (e.type != EventType.Repaint && e.type != EventType.MouseMove && e.type != EventType.MouseDown) return;

            Vector2 m = e.mousePosition;
            float best = EdgeClickDist;
            foreach (var edge in _edgeCache)
            {
                float distance = DistanceToBezier(m, edge, _sampleBuffer, 20);
                if (distance < best)
                {
                    best = distance;
                    hoverEdgeFrom = edge.fromId;
                    hoverEdgeTo = edge.toId;
                }
            }
        }

        private void DrawEdges()
        {
            foreach (var edge in _edgeCache)
            {
                var from = graph.GetNode(edge.fromId);
                if (from == null) continue;

                bool hot = edge.fromId == hoverEdgeFrom && edge.toId == hoverEdgeTo;
                bool selected = edge.fromId == selectedId || edge.toId == selectedId;
                bool runtimeOut = _rtActive && edge.fromId == _rtCurrentId;
                bool runtimeLast = _rtActive && edge.fromId == _rtLastFromId && edge.toId == _rtLastToId;
                bool runtimeVisited = _rtActive && _rtVisitedIds.Contains(edge.fromId) && _rtVisitedIds.Contains(edge.toId);

                Color color;
                float width;
                if (runtimeLast)
                {
                    color = new Color(0.35f, 0.75f, 1f, 1f);
                    width = 6.2f;
                }
                else if (runtimeOut)
                {
                    color = new Color(0.35f, 1f, 0.45f, 1f);
                    width = 5.5f;
                }
                else if (runtimeVisited)
                {
                    color = new Color(0.55f, 0.85f, 1f, 0.72f);
                    width = 3.4f;
                }
                else if (hot)
                {
                    color = new Color(1f, 0.35f, 0.25f, 1f);
                    width = 5f;
                }
                else if (edge.isBackEdge)
                {
                    color = new Color(1f, 0.62f, 0.22f, selected ? 1f : 0.82f);
                    width = selected ? 4f : 2.8f;
                }
                else if (from.actionType == TUTORIAL_ACTION.BRANCH)
                {
                    color = new Color(0.38f, 0.95f, 0.54f, selected ? 1f : 0.84f);
                    width = selected ? 4.2f : 3f;
                }
                else
                {
                    color = new Color(0.72f, 0.74f, 0.78f, selected ? 1f : 0.78f);
                    width = selected ? 4.2f : 2.8f;
                }

                Handles.DrawBezier(edge.p0, edge.p1, edge.c0, edge.c1, color, null, width);
                DrawArrowAt(edge, color, width);

                if (showEdgeLabels && !string.IsNullOrEmpty(edge.label))
                {
                    Vector3 mid = BezierPoint(edge.p0, edge.c0, edge.c1, edge.p1, 0.5f);
                    DrawEdgeLabel(mid, edge.label, from.actionType == TUTORIAL_ACTION.BRANCH);
                }
            }
        }

        private void DrawArrowAt(EdgeDrawInfo edge, Color color, float width)
        {
            Vector3 a = BezierPoint(edge.p0, edge.c0, edge.c1, edge.p1, 0.62f);
            Vector3 b = BezierPoint(edge.p0, edge.c0, edge.c1, edge.p1, 0.68f);
            Vector3 dir = b - a;
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
            float size = (7f + width) * zoom;
            Vector3 tip = b;
            Vector3 left = b - dir * size + perp * size * 0.55f;
            Vector3 right = b - dir * size - perp * size * 0.55f;
            Handles.color = color;
            Handles.DrawAAConvexPolygon(tip, left, right);
        }

        private void DrawEdgeLabel(Vector3 center, string label, bool important)
        {
            label = Truncate(label, 28);
            Vector2 size = _badgeStyle.CalcSize(new GUIContent(label));
            size.x = Mathf.Clamp(size.x + 14f, 58f, 190f);
            size.y = 19f;
            var rect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
            EditorGUI.DrawRect(rect, important ? new Color(0.05f, 0.24f, 0.08f, 0.90f) : new Color(0.08f, 0.09f, 0.10f, 0.88f));
            GUI.Label(rect, label, _badgeStyle);
        }

        private void DrawLinkingLine()
        {
            var from = graph.GetNode(linkFromId);
            if (from == null) return;
            Vector3 start = BottomPort(NodeRect(from));
            Vector3 end = Event.current.mousePosition;
            Vector3 c0 = start + Vector3.up * (90f * zoom);
            Vector3 c1 = end + Vector3.down * (90f * zoom);
            Handles.DrawBezier(start, end, c0, c1, new Color(0.35f, 0.75f, 1f), null, 3.5f);
        }

        private void DrawNode(TutorialGraphSO.TutorialStepNode node)
        {
            Rect rect = NodeRect(node);
            bool selected = node.id == selectedId;
            bool start = node.id == graph.startNodeId;
            bool runtime = _rtActive && node.id == _rtCurrentId;
            bool runtimeVisited = _rtActive && !runtime && _rtVisitedIds.Contains(node.id);
            bool reachableFromSelected = false;
            var sel = graph.GetNode(selectedId);
            if (sel != null) reachableFromSelected = sel.nextIds.Contains(node.id);

            Color color = NodeColor(node.actionType);
            if (Event.current.type == EventType.Repaint)
            {
                DrawNodeBackground(rect, color, selected, start, runtime, runtimeVisited, reachableFromSelected);
            }

            GUI.BeginGroup(rect);
            var prevContent = GUI.contentColor;
            GUI.contentColor = new Color(0.08f, 0.08f, 0.08f, 1f);

            GUI.Label(new Rect(8f * zoom, 4f * zoom, rect.width - 16f * zoom, TitleH - 4f * zoom),
                $"{node.actionType}  #{node.id}", _titleStyle);

            if (showNodeDetails && zoom >= 0.58f)
            {
                float y = TitleH + 8f * zoom;
                float line = 17f * zoom;
                GUI.Label(new Rect(10f * zoom, y, rect.width - 20f * zoom, line),
                    string.IsNullOrEmpty(node.conditionKey) ? "condition: —" : $"when: {Truncate(node.conditionKey, 32)}",
                    _miniCenterStyle);
                y += line;

                string actionText = BuildNodeActionSummary(node);
                GUI.Label(new Rect(10f * zoom, y, rect.width - 20f * zoom, line), actionText, _miniCenterStyle);
                y += line;

                string runtimeKey = node.tutorialStepIndex >= 0
                    ? $"L{node.tutorialLevel} · step {node.tutorialStepIndex}"
                    : "step —";
                string flow = $"{runtimeKey} · rank {node.layer}" + (node.delay > 0 ? $" · {node.delay:0.##}s" : string.Empty);
                if (start) flow += " · START";
                GUI.Label(new Rect(10f * zoom, y, rect.width - 20f * zoom, line), flow, _miniCenterStyle);
            }

            GUI.contentColor = prevContent;
            GUI.EndGroup();

            DrawPorts(node, rect, selected);
            HandleNodeEvents(node, rect, BottomPortRect(rect));
        }

        private string BuildNodeActionSummary(TutorialGraphSO.TutorialStepNode node)
        {
            if (node.action != null)
            {
                switch (node.actionType)
                {
                    case TUTORIAL_ACTION.SHOW_CONTENT_STRING:
                        return string.IsNullOrEmpty(node.action.ContentString) ? "content: —" : Truncate(node.action.ContentString, 32);
                    case TUTORIAL_ACTION.SHOW_VIDEO:
                        return string.IsNullOrEmpty(node.action.Description) ? "video" : Truncate(node.action.Description, 32);
                    case TUTORIAL_ACTION.HAND:
                        return $"hand: {node.action.Action}";
                    case TUTORIAL_ACTION.BRANCH:
                        return node.action.Conditions == null ? "branch" : $"branch paths: {node.action.Conditions.Count}";
                }
            }

            int post = node.postProcessKeys == null ? 0 : node.postProcessKeys.Count;
            int other = node.otherActionKeys == null ? 0 : node.otherActionKeys.Count;
            return $"post {post} · other {other} · next {node.nextIds.Count}";
        }

        private void DrawNodeBackground(Rect rect, Color color, bool selected, bool start, bool runtime, bool runtimeVisited, bool child)
        {
            EditorGUI.DrawRect(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
            EditorGUI.DrawRect(rect, color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, TitleH), new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, 1f));

            Color outline = new Color(0f, 0f, 0f, 0.35f);
            float outlineW = 1f;
            if (runtimeVisited)
            {
                outline = new Color(0.45f, 0.72f, 1f, 0.95f);
                outlineW = 3f;
            }
            if (child)
            {
                outline = new Color(0.3f, 0.9f, 0.35f, 1f);
                outlineW = 3f;
            }
            if (start)
            {
                outline = new Color(1f, 0.92f, 0.35f, 1f);
                outlineW = 4f;
            }
            if (selected)
            {
                outline = new Color(0.22f, 0.85f, 0.36f, 1f);
                outlineW = 5f;
            }
            if (runtime)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 4f);
                outline = Color.Lerp(new Color(1f, 0.75f, 0.1f), new Color(1f, 1f, 0.55f), pulse);
                outlineW = 6f;
            }

            DrawRectOutline(rect, outline, outlineW);
        }

        private void DrawPorts(TutorialGraphSO.TutorialStepNode node, Rect rect, bool selected)
        {
            Vector3 bottom = BottomPort(rect);
            Vector3 top = TopPort(rect);

            Color portFill = selected ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.92f, 0.92f, 0.92f);
            Handles.color = new Color(0.18f, 0.18f, 0.18f);
            Handles.DrawSolidDisc(top, Vector3.forward, PortR * 0.72f);
            Handles.color = portFill;
            Handles.DrawSolidDisc(bottom, Vector3.forward, PortR);
            Handles.color = new Color(0.15f, 0.15f, 0.15f);
            Handles.DrawWireDisc(bottom, Vector3.forward, PortR);

            EditorGUIUtility.AddCursorRect(BottomPortRect(rect), MouseCursor.ArrowPlus);
        }

        private Rect BottomPortRect(Rect rect)
        {
            Vector3 p = BottomPort(rect);
            float r = Mathf.Max(9f, PortR + 5f);
            return new Rect(p.x - r, p.y - r, r * 2f, r * 2f);
        }

        private void HandleNodeEvents(TutorialGraphSO.TutorialStepNode node, Rect rect, Rect linkPort)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && linkPort.Contains(e.mousePosition))
            {
                selectedId = node.id;
                isLinking = true;
                linkFromId = node.id;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp && isLinking && rect.Contains(e.mousePosition))
            {
                if (linkFromId != node.id) TryConnect(linkFromId, node.id);
                isLinking = false;
                linkFromId = -1;
                e.Use();
                return;
            }

            if (!rect.Contains(e.mousePosition)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                selectedId = node.id;
                draggingId = node.id;
                dragStarted = false;
                dragMouseOffset = e.mousePosition - rect.position;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && draggingId == node.id)
            {
                if (!dragStarted)
                {
                    Undo.RecordObject(graph, "Move Tutorial Node");
                    EnsureManualLayoutBeforeMutation();
                    dragStarted = true;
                }

                Vector2 displayPos = e.mousePosition - dragMouseOffset;
                Vector2 graphPos = displayPos / zoom;
                if (snapToGrid) graphPos = Snap(graphPos, BaseGrid);

                node.useManualPosition = true;
                node.editorPosition = graphPos;
                UpdateLogicalOrderFromPosition(node);
                EditorUtility.SetDirty(graph);
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && draggingId == node.id)
            {
                NormalizeSlotsForLayer(node.layer);
                draggingId = -1;
                dragStarted = false;
                e.Use();
            }
            else if (e.type == EventType.ContextClick)
            {
                ShowNodeContextMenu(node);
                e.Use();
            }
        }

        private void HandleCanvasEvents()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseUp && draggingId >= 0)
            {
                var dragged = graph.GetNode(draggingId);
                if (dragged != null) NormalizeSlotsForLayer(dragged.layer);
                draggingId = -1;
                dragStarted = false;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp && isLinking)
            {
                isLinking = false;
                linkFromId = -1;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && hoverEdgeFrom >= 0 && !AnyNodeContains(e.mousePosition))
            {
                DeleteEdge(hoverEdgeFrom, hoverEdgeTo);
                hoverEdgeFrom = hoverEdgeTo = -1;
                e.Use();
                return;
            }

            if (e.type == EventType.ContextClick)
            {
                Vector2 graphPos = e.mousePosition / zoom;
                if (snapToGrid) graphPos = Snap(graphPos, BaseGrid);
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Node Here"), false, () => AddNodeAt(graphPos));
                if (selectedId >= 0)
                {
                    menu.AddItem(new GUIContent("Add Child From Selected Here"), false, () => AddChildAtSelected(graphPos));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Add Child From Selected Here"));
                }
                menu.ShowAsContext();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !AnyNodeContains(e.mousePosition))
            {
                if (selectedId >= 0)
                {
                    selectedId = -1;
                    Repaint();
                }
            }
        }

        private bool AnyNodeContains(Vector2 p)
        {
            foreach (var node in graph.nodes)
                if (NodeRect(node).Contains(p)) return true;
            return false;
        }

        private void HandlePan(Rect area)
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if ((e.button == 2 || (e.button == 0 && (e.alt || e.shift))) && area.Contains(e.mousePosition))
                    {
                        panning = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (panning)
                    {
                        canvasScroll -= e.delta;
                        canvasScroll.x = Mathf.Max(0, canvasScroll.x);
                        canvasScroll.y = Mathf.Max(0, canvasScroll.y);
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (panning)
                    {
                        panning = false;
                        e.Use();
                    }
                    break;
            }
        }

        private void HandleWheelZoom(Rect area)
        {
            Event e = Event.current;
            if (e.type != EventType.ScrollWheel || !area.Contains(e.mousePosition)) return;

            float oldZoom = zoom;
            float newZoom = Mathf.Clamp(zoom - e.delta.y * 0.03f, 0.45f, 1.6f);
            if (Mathf.Approximately(oldZoom, newZoom)) return;

            Vector2 mouseInArea = e.mousePosition - new Vector2(area.x, area.y);
            Vector2 mouseContent = canvasScroll + mouseInArea;
            float factor = newZoom / oldZoom;
            canvasScroll = mouseContent * factor - mouseInArea;
            canvasScroll.x = Mathf.Max(0, canvasScroll.x);
            canvasScroll.y = Mathf.Max(0, canvasScroll.y);
            zoom = newZoom;
            e.Use();
            Repaint();
        }

        private void HandleSplitter(Rect splitter)
        {
            EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(splitter, new Color(0f, 0f, 0f, 0.22f));

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && splitter.Contains(e.mousePosition))
            {
                _draggingSplitter = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _draggingSplitter)
            {
                _inspectorW = Mathf.Clamp(_inspectorW - e.delta.x, InspectorMinW, InspectorMaxW);
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp)
            {
                _draggingSplitter = false;
            }
        }

        private void HandleKeyboardShortcuts()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Escape)
            {
                isLinking = false;
                linkFromId = -1;
                draggingId = -1;
                e.Use();
                Repaint();
            }
            else if (e.keyCode == KeyCode.F && selectedId >= 0)
            {
                FocusNode(selectedId);
                e.Use();
            }
            else if ((e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) && selectedId >= 0)
            {
                var node = graph.GetNode(selectedId);
                if (node != null) DeleteNode(node);
                e.Use();
            }
        }

        private void DrawLegendColumn(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, 18), "Tutorial Flow", EditorStyles.boldLabel);

            int y = 30;
            DrawLegendHint(rect, ref y, "Body drag", "move node freely");
            DrawLegendHint(rect, ref y, "Bottom dot", "create link");
            DrawLegendHint(rect, ref y, "Edge click", "delete link");
            DrawLegendHint(rect, ref y, "F", "focus selected");
            y += 8;

            GUI.Label(new Rect(rect.x + 8, rect.y + y, rect.width - 16, 18), "Action Types", EditorStyles.boldLabel);
            y += 24;
            var types = (TUTORIAL_ACTION[])System.Enum.GetValues(typeof(TUTORIAL_ACTION));
            for (int i = 0; i < types.Length; i++)
            {
                var row = new Rect(rect.x + 8, rect.y + y + i * 22, rect.width - 16, 20);
                var swatch = new Rect(row.x, row.y + 3, 14, 14);
                EditorGUI.DrawRect(swatch, NodeColor(types[i]));
                GUI.Label(new Rect(row.x + 20, row.y, row.width - 20, 18), types[i].ToString(), EditorStyles.miniLabel);
            }
        }

        private void DrawLegendHint(Rect rect, ref int y, string left, string right)
        {
            GUI.Label(new Rect(rect.x + 8, rect.y + y, 68, 18), left, EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(rect.x + 76, rect.y + y, rect.width - 84, 18), right, EditorStyles.miniLabel);
            y += 19;
        }

        private void DrawMinimap(Rect legendRect)
        {
            if (graph.nodes.Count == 0) return;

            float mmW = legendRect.width - 10f;
            float mmH = mmW * 0.72f;
            var mm = new Rect(legendRect.x + 5f, legendRect.yMax - mmH - 12f, mmW, mmH);
            GUI.Box(mm, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(new Rect(mm.x + 5, mm.y + 2, mm.width - 10, 14), "Minimap", EditorStyles.miniLabel);

            float innerW = mm.width - 10f;
            float innerH = mm.height - 22f;
            if (_contentW <= 0 || _contentH <= 0) return;

            float scale = Mathf.Min(innerW / _contentW, innerH / _contentH);
            Vector2 origin = new Vector2(mm.x + 5f + (innerW - _contentW * scale) * 0.5f, mm.y + 19f);

            foreach (var node in graph.nodes)
            {
                Rect r = NodeRect(node);
                var dot = new Rect(origin.x + r.x * scale, origin.y + r.y * scale,
                    Mathf.Max(3f, r.width * scale), Mathf.Max(2f, r.height * scale));
                EditorGUI.DrawRect(dot, NodeColor(node.actionType));
            }

            var view = new Rect(origin.x + canvasScroll.x * scale, origin.y + canvasScroll.y * scale,
                _canvasAreaRect.width * scale, _canvasAreaRect.height * scale);
            Handles.DrawSolidRectangleWithOutline(view, new Color(1, 1, 1, 0.06f), new Color(1, 1, 1, 0.65f));
        }

        private void DrawInspector(Rect area)
        {
            GUILayout.BeginArea(area, EditorStyles.helpBox);

            if (so == null) RefreshSerialized();
            if (so != null)
            {
                so.Update();
                SerializedProperty levelProp = so.FindProperty("tutorialLevel");
                if (levelProp != null)
                    EditorGUILayout.PropertyField(levelProp, new GUIContent("Graph Runtime Level"));
                so.ApplyModifiedProperties();
                EditorGUILayout.Space(4);
            }

            var node = graph.GetNode(selectedId);
            if (node == null)
            {
                EditorGUILayout.LabelField("No node selected", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Click a node to inspect it. Right-click the canvas to add nodes. Use Auto Layout to rebuild a clean flow from existing links. Use RT Ids once for graph assets created before this runtime-follow patch.", MessageType.None);
                GUILayout.EndArea();
                return;
            }

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            EditorGUILayout.LabelField($"{node.actionType}  #{node.id}", EditorStyles.boldLabel);
            if (node.id == graph.startNodeId)
                EditorGUILayout.HelpBox("Start node", MessageType.None);

            int index = graph.nodes.IndexOf(node);
            if (so == null) RefreshSerialized();
            so.Update();
            SerializedProperty nodeProp = so.FindProperty("nodes").GetArrayElementAtIndex(index);

            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("conditionKey"), new GUIContent("Condition Key"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("actionType"), new GUIContent("Action Type"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("delay"), new GUIContent("Delay"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("tutorialLevel"), new GUIContent("Runtime Level"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("tutorialStepIndex"), new GUIContent("Runtime Step Index"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("layer"), new GUIContent("Rank / Layer"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("slot"), new GUIContent("Slot"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("useManualPosition"), new GUIContent("Use Saved Position"));
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("editorPosition"), new GUIContent("Editor Position"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Cached Action Keys", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("postProcessKeys"), true);
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("otherActionKeys"), true);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Full Action Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nodeProp.FindPropertyRelative("action"), true);

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull From Action"))
            {
                Undo.RecordObject(graph, "Pull Node Data From Action");
                SyncNodeFromAction(node);
                Commit();
            }
            if (GUILayout.Button("Push To Action"))
            {
                Undo.RecordObject(graph, "Push Node Cache To Action");
                SyncNodeToAction(node);
                Commit();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
            for (int i = node.nextIds.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                var child = graph.GetNode(node.nextIds[i]);
                string label = child != null ? $"→ #{child.id} {child.actionType}" : $"→ {node.nextIds[i]} (missing)";
                EditorGUILayout.LabelField(label);
                if (GUILayout.Button("x", GUILayout.Width(24)))
                {
                    Undo.RecordObject(graph, "Remove Tutorial Edge");
                    node.nextIds.RemoveAt(i);
                    Commit();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Add Child Below")) AddChild(node);
            if (GUILayout.Button("Focus Node")) FocusNode(node.id);
            if (GUILayout.Button("Set As Start"))
            {
                Undo.RecordObject(graph, "Set Tutorial Start Node");
                graph.startNodeId = node.id;
                Commit();
            }

            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button("Delete Node")) DeleteNode(node);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void AddRoot()
        {
            Undo.RecordObject(graph, "Add Tutorial Root");
            EnsureManualLayoutBeforeMutation();
            var node = NewNode(new Vector2(BaseMargin, BaseMargin));
            node.actionType = TUTORIAL_ACTION.NONE;
            if (graph.startNodeId < 0 || graph.GetNode(graph.startNodeId) == null) graph.startNodeId = node.id;
            selectedId = node.id;
            Commit();
            FocusNode(node.id);
        }

        private void AddNodeAt(Vector2 graphPosition)
        {
            Undo.RecordObject(graph, "Add Tutorial Node");
            EnsureManualLayoutBeforeMutation();
            var node = NewNode(graphPosition);
            selectedId = node.id;
            Commit();
        }

        private void AddChildAtSelected(Vector2 graphPosition)
        {
            var parent = graph.GetNode(selectedId);
            if (parent == null) return;
            Undo.RecordObject(graph, "Add Tutorial Child");
            EnsureManualLayoutBeforeMutation();
            var child = NewNode(graphPosition);
            parent.nextIds.Add(child.id);
            selectedId = child.id;
            Commit();
        }

        private void AddChild(TutorialGraphSO.TutorialStepNode parent)
        {
            Undo.RecordObject(graph, "Add Tutorial Child");
            EnsureManualLayoutBeforeMutation();
            Vector2 parentPos = GetGraphPosition(parent);
            Vector2 childPos = parentPos + new Vector2(0f, BaseRankGapY);
            if (snapToGrid) childPos = Snap(childPos, BaseGrid);
            var child = NewNode(childPos);
            child.layer = parent.layer + 1;
            child.slot = NextFreeSlot(child.layer);
            if (!parent.nextIds.Contains(child.id)) parent.nextIds.Add(child.id);
            selectedId = child.id;
            Commit();
        }

        private TutorialGraphSO.TutorialStepNode NewNode(Vector2 graphPosition)
        {
            var node = new TutorialGraphSO.TutorialStepNode
            {
                id = graph.AllocId(),
                tutorialLevel = graph.tutorialLevel,
                tutorialStepIndex = NextRuntimeStepIndex(),
                actionType = TUTORIAL_ACTION.HAND,
                useManualPosition = true,
                editorPosition = graphPosition,
                postProcessKeys = new List<string>(),
                otherActionKeys = new List<string>(),
                nextIds = new List<int>()
            };
            UpdateLogicalOrderFromPosition(node);
            graph.nodes.Add(node);
            return node;
        }

        private int NextFreeSlot(int layer)
        {
            int slot = 0;
            var used = new HashSet<int>();
            foreach (var node in graph.nodes)
                if (node.layer == layer) used.Add(node.slot);
            while (used.Contains(slot)) slot++;
            return slot;
        }

        private int NextRuntimeStepIndex()
        {
            int max = -1;
            if (graph == null) return 0;
            foreach (var node in graph.nodes)
                if (node.tutorialStepIndex > max) max = node.tutorialStepIndex;
            return max + 1;
        }

        private void TryConnect(int fromId, int toId)
        {
            var from = graph.GetNode(fromId);
            var to = graph.GetNode(toId);
            if (from == null || to == null || fromId == toId) return;
            if (from.nextIds.Contains(toId)) return;

            Undo.RecordObject(graph, "Connect Tutorial Nodes");
            from.nextIds.Add(toId);
            Commit();
        }

        private void DeleteEdge(int fromId, int toId)
        {
            var from = graph.GetNode(fromId);
            if (from == null) return;
            Undo.RecordObject(graph, "Delete Tutorial Edge");
            from.nextIds.RemoveAll(id => id == toId);
            Commit();
        }

        private void DeleteNode(TutorialGraphSO.TutorialStepNode node)
        {
            if (node == null) return;
            Undo.RecordObject(graph, "Delete Tutorial Node");
            graph.nodes.Remove(node);
            foreach (var other in graph.nodes) other.nextIds.RemoveAll(id => id == node.id);
            if (graph.startNodeId == node.id) graph.startNodeId = graph.nodes.Count > 0 ? graph.nodes[0].id : -1;
            if (selectedId == node.id) selectedId = -1;
            Commit();
        }

        private void CleanMissingEdges()
        {
            Undo.RecordObject(graph, "Clean Tutorial Missing Edges");
            var ids = new HashSet<int>();
            foreach (var node in graph.nodes) ids.Add(node.id);
            foreach (var node in graph.nodes) node.nextIds.RemoveAll(id => !ids.Contains(id) || id == node.id);
            Commit();
        }

        private void BackfillRuntimeIdsFromCurrentOrder()
        {
            if (graph == null || graph.nodes.Count == 0) return;
            Undo.RecordObject(graph, "Backfill Tutorial Runtime IDs");

            int resolvedLevel = ResolveGraphLevelFromAssetName();
            graph.tutorialLevel = resolvedLevel;

            var ordered = new List<TutorialGraphSO.TutorialStepNode>(graph.nodes);
            ordered.Sort((a, b) =>
            {
                if (a.layer != b.layer) return a.layer.CompareTo(b.layer);
                if (a.slot != b.slot) return a.slot.CompareTo(b.slot);
                return a.id.CompareTo(b.id);
            });

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].tutorialLevel = resolvedLevel;
                ordered[i].tutorialStepIndex = i;
            }

            Commit();
        }

        private int ResolveGraphLevelFromAssetName()
        {
            if (graph == null) return -1;
            int current = graph.tutorialLevel;
            string path = AssetDatabase.GetAssetPath(graph);
            string name = string.IsNullOrEmpty(path) ? graph.name : System.IO.Path.GetFileNameWithoutExtension(path);
            int level = TryParseLevelSuffix(name, current);
            return level >= 0 ? level : current;
        }

        private static int TryParseLevelSuffix(string name, int fallback)
        {
            if (string.IsNullOrEmpty(name)) return fallback;
            int idx = name.LastIndexOf("Level", System.StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return fallback;
            idx += 5;
            while (idx < name.Length && !char.IsDigit(name[idx])) idx++;
            if (idx >= name.Length) return fallback;

            int value = 0;
            bool any = false;
            while (idx < name.Length && char.IsDigit(name[idx]))
            {
                any = true;
                value = value * 10 + (name[idx] - '0');
                idx++;
            }

            return any ? value : fallback;
        }

        private void SyncAllFromActions()
        {
            Undo.RecordObject(graph, "Sync Tutorial Graph From Action Data");
            foreach (var node in graph.nodes) SyncNodeFromAction(node);
            Commit();
        }

        private void SyncNodeFromAction(TutorialGraphSO.TutorialStepNode node)
        {
            if (node == null || node.action == null) return;
            node.actionType = node.action.Type;
            node.delay = node.action.Delay;
            node.postProcessKeys = node.action.PostProcessActions != null
                ? new List<string>(node.action.PostProcessActions)
                : new List<string>();
            node.otherActionKeys = node.action.OtherActions != null
                ? new List<string>(node.action.OtherActions)
                : new List<string>();
        }

        private void SyncNodeToAction(TutorialGraphSO.TutorialStepNode node)
        {
            if (node == null || node.action == null) return;
            node.action.Type = node.actionType;
            node.action.Delay = node.delay;
            node.action.PostProcessActions = node.postProcessKeys != null
                ? new List<string>(node.postProcessKeys)
                : new List<string>();
            node.action.OtherActions = node.otherActionKeys != null
                ? new List<string>(node.otherActionKeys)
                : new List<string>();
        }

        private void AutoLayout()
        {
            if (graph == null || graph.nodes.Count == 0) return;

            Undo.RecordObject(graph, "Auto Layout Tutorial Graph");
            ComputeBackEdges();

            var rankMap = CalculateRanksIgnoringBackEdges();
            var byRank = new Dictionary<int, List<TutorialGraphSO.TutorialStepNode>>();
            foreach (var node in graph.nodes)
            {
                int rank = rankMap.ContainsKey(node.id) ? rankMap[node.id] : 0;
                if (!byRank.ContainsKey(rank)) byRank[rank] = new List<TutorialGraphSO.TutorialStepNode>();
                byRank[rank].Add(node);
            }

            var slotMap = new Dictionary<int, float>();
            int maxRank = 0;
            foreach (var kvp in byRank)
            {
                maxRank = Mathf.Max(maxRank, kvp.Key);
                kvp.Value.Sort((a, b) => GetGraphPosition(a).x.CompareTo(GetGraphPosition(b).x));
                for (int i = 0; i < kvp.Value.Count; i++) slotMap[kvp.Value[i].id] = i;
            }

            for (int pass = 0; pass < 4; pass++)
            {
                bool downward = pass % 2 == 0;
                int startRank = downward ? 1 : maxRank - 1;
                int endRank = downward ? maxRank + 1 : -1;
                int step = downward ? 1 : -1;

                for (int rank = startRank; rank != endRank; rank += step)
                {
                    if (!byRank.ContainsKey(rank)) continue;
                    var row = byRank[rank];
                    foreach (var node in row)
                    {
                        float sum = 0f;
                        int count = 0;
                        foreach (var other in graph.nodes)
                        {
                            if (rankMap[other.id] == (downward ? rank - 1 : rank + 1) && other.nextIds.Contains(node.id))
                            {
                                sum += slotMap[other.id];
                                count++;
                            }
                        }
                        foreach (int childId in node.nextIds)
                        {
                            if (!slotMap.ContainsKey(childId) || !rankMap.ContainsKey(childId)) continue;
                            if (rankMap[childId] == (downward ? rank - 1 : rank + 1))
                            {
                                sum += slotMap[childId];
                                count++;
                            }
                        }
                        if (count > 0) slotMap[node.id] = sum / count;
                    }
                    row.Sort((a, b) => slotMap[a.id].CompareTo(slotMap[b.id]));
                    for (int i = 0; i < row.Count; i++) slotMap[row[i].id] = i;
                }
            }

            int maxCount = 1;
            foreach (var kvp in byRank) maxCount = Mathf.Max(maxCount, kvp.Value.Count);
            float visibleGraphW = Mathf.Max(900f, _canvasAreaRect.width > 1f ? _canvasAreaRect.width / zoom : 900f);
            float contentGraphW = Mathf.Max(visibleGraphW, BaseMargin * 2f + BaseNodeW + (maxCount - 1) * BaseSiblingGapX);
            float centerX = contentGraphW * 0.5f;

            for (int rank = 0; rank <= maxRank; rank++)
            {
                if (!byRank.ContainsKey(rank)) continue;
                var row = byRank[rank];
                row.Sort((a, b) => slotMap[a.id].CompareTo(slotMap[b.id]));
                for (int i = 0; i < row.Count; i++)
                {
                    var node = row[i];
                    node.layer = rank;
                    node.slot = i;
                    node.useManualPosition = true;
                    Vector2 pos = new Vector2(
                        centerX + (i - (row.Count - 1) * 0.5f) * BaseSiblingGapX - BaseNodeW * 0.5f,
                        BaseMargin + rank * BaseRankGapY);
                    node.editorPosition = snapToGrid ? Snap(pos, BaseGrid) : pos;
                }
            }

            Commit();
            FitToGraph();
        }

        private Dictionary<int, int> CalculateRanksIgnoringBackEdges()
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
                    if (_backEdges.Contains(new EdgeKey(node.id, childId))) continue;
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
                if (visited.Contains(id)) continue;
                visited.Add(id);
                foreach (int childId in adj[id])
                {
                    rank[childId] = Mathf.Max(rank[childId], rank[id] + 1);
                    inDegree[childId]--;
                    if (inDegree[childId] <= 0) queue.Enqueue(childId);
                }
            }

            int fallbackRank = graph.nodes.Count + 1;
            foreach (var node in graph.nodes)
                if (!visited.Contains(node.id)) rank[node.id] = fallbackRank++;

            return rank;
        }

        private void ComputeBackEdges()
        {
            _backEdges.Clear();
            if (graph == null || graph.nodes.Count == 0) return;

            int startId = graph.GetNode(graph.startNodeId) != null ? graph.startNodeId : graph.nodes[0].id;
            var visited = new HashSet<int>();
            var stack = new HashSet<int>();
            Dfs(startId);
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
                        if (stack.Contains(childId)) _backEdges.Add(new EdgeKey(id, childId));
                        else if (!visited.Contains(childId)) Dfs(childId);
                    }
                }
                stack.Remove(id);
            }
        }

        private void NormalizeSlotsForLayer(int layer)
        {
            var row = new List<TutorialGraphSO.TutorialStepNode>();
            foreach (var node in graph.nodes)
                if (node.layer == layer) row.Add(node);
            row.Sort((a, b) => GetGraphPosition(a).x.CompareTo(GetGraphPosition(b).x));
            for (int i = 0; i < row.Count; i++) row[i].slot = i;
            EditorUtility.SetDirty(graph);
        }

        private void UpdateLogicalOrderFromPosition(TutorialGraphSO.TutorialStepNode node)
        {
            Vector2 pos = GetGraphPosition(node);
            node.layer = Mathf.Max(0, Mathf.RoundToInt((pos.y - BaseMargin) / BaseRankGapY));
            node.slot = Mathf.Max(0, Mathf.RoundToInt((pos.x - BaseMargin) / BaseSiblingGapX));
        }

        private void ShowNodeContextMenu(TutorialGraphSO.TutorialStepNode node)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Child Below"), false, () => AddChild(node));
            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateNode(node));
            menu.AddItem(new GUIContent("Set As Start"), node.id == graph.startNodeId, () =>
            {
                Undo.RecordObject(graph, "Set Tutorial Start Node");
                graph.startNodeId = node.id;
                Commit();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Pull From Action"), false, () =>
            {
                Undo.RecordObject(graph, "Pull Node Data From Action");
                SyncNodeFromAction(node);
                Commit();
            });
            menu.AddItem(new GUIContent("Push To Action"), false, () =>
            {
                Undo.RecordObject(graph, "Push Node Cache To Action");
                SyncNodeToAction(node);
                Commit();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(node));
            menu.ShowAsContext();
        }

        private void DuplicateNode(TutorialGraphSO.TutorialStepNode src)
        {
            Undo.RecordObject(graph, "Duplicate Tutorial Node");
            EnsureManualLayoutBeforeMutation();
            var copy = new TutorialGraphSO.TutorialStepNode
            {
                id = graph.AllocId(),
                tutorialLevel = src.tutorialLevel,
                tutorialStepIndex = NextRuntimeStepIndex(),
                layer = src.layer,
                slot = NextFreeSlot(src.layer),
                nextIds = new List<int>(),
                conditionKey = src.conditionKey,
                actionType = src.actionType,
                action = src.action,
                delay = src.delay,
                postProcessKeys = src.postProcessKeys != null ? new List<string>(src.postProcessKeys) : new List<string>(),
                otherActionKeys = src.otherActionKeys != null ? new List<string>(src.otherActionKeys) : new List<string>(),
                useManualPosition = true,
                editorPosition = GetGraphPosition(src) + new Vector2(BaseGrid * 2f, BaseGrid * 2f)
            };
            graph.nodes.Add(copy);
            selectedId = copy.id;
            Commit();
        }

        private void FitToGraph()
        {
            if (graph == null || graph.nodes.Count == 0) return;
            BuildLayout(_canvasAreaRect.width > 1f ? _canvasAreaRect : new Rect(0, 0, position.width, position.height));

            Rect bounds = GetGraphBounds();
            canvasScroll.x = Mathf.Max(0, bounds.xMin - 120f);
            canvasScroll.y = Mathf.Max(0, bounds.yMin - 120f);
            Repaint();
        }

        private void FocusNode(int id)
        {
            if (!_layout.TryGetValue(id, out var rect)) BuildLayout(_canvasAreaRect);
            if (!_layout.TryGetValue(id, out rect)) return;
            canvasScroll.x = Mathf.Max(0, rect.center.x - _canvasAreaRect.width * 0.5f);
            canvasScroll.y = Mathf.Max(0, rect.center.y - _canvasAreaRect.height * 0.5f);
            Repaint();
        }

        private void MaybeAutoFocusRuntimeNode()
        {
            if (!autoFocusRuntime || !_pendingRuntimeFocus || _rtCurrentId < 0) return;
            if (_rtLastFocusedId == _rtCurrentId)
            {
                _pendingRuntimeFocus = false;
                return;
            }

            FocusNode(_rtCurrentId);
            selectedId = _rtCurrentId;
            _rtLastFocusedId = _rtCurrentId;
            _pendingRuntimeFocus = false;
        }

        private int FindRuntimeNodeId(int runtimeLevel, int runtimeStep)
        {
            if (graph == null || runtimeStep < 0) return -1;

            // If this graph is explicitly bound to another tutorial level, do not highlight a wrong graph.
            if (graph.tutorialLevel >= 0 && runtimeLevel >= 0 && graph.tutorialLevel != runtimeLevel)
                return -1;

            bool hasStepMetadata = false;
            bool hasAnyLevelMatch = graph.tutorialLevel < 0 || graph.tutorialLevel == runtimeLevel;
            foreach (var node in graph.nodes)
            {
                if (node.tutorialStepIndex >= 0) hasStepMetadata = true;
                if (node.tutorialLevel == runtimeLevel) hasAnyLevelMatch = true;
            }

            // Best case: graph and node were migrated from TutorialDataSO and both level + step are stable.
            foreach (var node in graph.nodes)
            {
                if (node.tutorialStepIndex == runtimeStep && node.tutorialLevel == runtimeLevel)
                    return node.id;
            }

            // Graph generated per level: allow graph-level match even if older nodes have tutorialLevel unset.
            if (hasAnyLevelMatch)
            {
                foreach (var node in graph.nodes)
                {
                    if (node.tutorialStepIndex == runtimeStep)
                        return node.id;
                }
            }

            // Legacy fallback for old graph assets that do not have tutorialStepIndex yet.
            // This keeps old Dungeon-derived graphs usable, but only when no reliable runtime metadata exists.
            if (hasStepMetadata) return -1;

            var ordered = new List<TutorialGraphSO.TutorialStepNode>(graph.nodes);
            ordered.Sort((a, b) =>
            {
                if (a.layer != b.layer) return a.layer.CompareTo(b.layer);
                if (a.slot != b.slot) return a.slot.CompareTo(b.slot);
                return a.id.CompareTo(b.id);
            });

            if (runtimeStep >= 0 && runtimeStep < ordered.Count) return ordered[runtimeStep].id;
            return -1;
        }

        private bool TryResolveRuntimeTransition(int previousId, int currentId, out int fromId, out int toId)
        {
            fromId = -1;
            toId = -1;

            var previous = graph.GetNode(previousId);
            if (previous != null && previous.nextIds.Contains(currentId))
            {
                fromId = previousId;
                toId = currentId;
                return true;
            }

            foreach (var node in graph.nodes)
            {
                if (!node.nextIds.Contains(currentId)) continue;
                if (!_rtVisitedIds.Contains(node.id) && node.id != previousId) continue;
                fromId = node.id;
                toId = currentId;
                return true;
            }

            return false;
        }

        private Rect GetGraphBounds()
        {
            if (graph.nodes.Count == 0) return new Rect(0, 0, 1, 1);
            bool first = true;
            Rect bounds = default;
            foreach (var node in graph.nodes)
            {
                Rect r = NodeRect(node);
                if (first)
                {
                    bounds = r;
                    first = false;
                }
                else
                {
                    float minX = Mathf.Min(bounds.xMin, r.xMin);
                    float minY = Mathf.Min(bounds.yMin, r.yMin);
                    float maxX = Mathf.Max(bounds.xMax, r.xMax);
                    float maxY = Mathf.Max(bounds.yMax, r.yMax);
                    bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
                }
            }
            return bounds;
        }

        private void SaveGraph()
        {
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        private void Commit()
        {
            EditorUtility.SetDirty(graph);
            RefreshSerialized();
            Repaint();
        }

        private static Vector2 Snap(Vector2 value, float grid)
        {
            if (grid <= 0f) return value;
            return new Vector2(Mathf.Round(value.x / grid) * grid, Mathf.Round(value.y / grid) * grid);
        }

        private static Vector3 BezierPoint(Vector3 p0, Vector3 c0, Vector3 c1, Vector3 p1, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * c0 + 3f * u * t * t * c1 + t * t * t * p1;
        }

        private static float DistanceToBezier(Vector2 point, EdgeDrawInfo edge, Vector3[] buffer, int segments)
        {
            segments = Mathf.Clamp(segments, 2, buffer.Length - 1);
            for (int i = 0; i <= segments; i++)
                buffer[i] = BezierPoint(edge.p0, edge.c0, edge.c1, edge.p1, (float)i / segments);

            float best = float.MaxValue;
            for (int i = 0; i < segments; i++)
            {
                float d = DistancePointToSegment(point, buffer[i], buffer[i + 1]);
                if (d < best) best = d;
            }
            return best;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = Vector2.Dot(ab, ab);
            if (denom <= 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / denom);
            return Vector2.Distance(p, a + ab * t);
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, Mathf.Max(0, max - 1)) + "…";
        }

        private static void DrawRectOutline(Rect rect, Color color, float width)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.yMin, width, rect.height), color);
        }

        private static Color NodeColor(TUTORIAL_ACTION actionType)
        {
            switch (actionType)
            {
                case TUTORIAL_ACTION.HAND: return new Color(0.30f, 0.56f, 0.92f);
                case TUTORIAL_ACTION.SHOW_VIDEO: return new Color(0.86f, 0.50f, 0.22f);
                case TUTORIAL_ACTION.CALL_OUT: return new Color(0.95f, 0.78f, 0.26f);
                case TUTORIAL_ACTION.BRANCH: return new Color(0.42f, 0.78f, 0.44f);
                case TUTORIAL_ACTION.END_TUTORIAL: return new Color(0.82f, 0.33f, 0.30f);
                case TUTORIAL_ACTION.SHOW_CONTENT:
                case TUTORIAL_ACTION.SHOW_CONTENT_STRING: return new Color(0.62f, 0.45f, 0.84f);
                case TUTORIAL_ACTION.CLOSE_ALL: return new Color(0.46f, 0.46f, 0.52f);
                case TUTORIAL_ACTION.NONE: return new Color(0.55f, 0.58f, 0.62f);
                default: return new Color(0.50f, 0.50f, 0.55f);
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            _miniCenterStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            _badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.95f, 0.92f) },
                clipping = TextClipping.Clip
            };
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

            public override bool Equals(object obj)
            {
                if (!(obj is EdgeKey)) return false;
                var other = (EdgeKey)obj;
                return other._from == _from && other._to == _to;
            }

            public override int GetHashCode() => (_from * 397) ^ _to;
        }

        private struct EdgeDrawInfo
        {
            public int fromId;
            public int toId;
            public int childIndex;
            public Vector3 p0;
            public Vector3 p1;
            public Vector3 c0;
            public Vector3 c1;
            public string label;
            public bool isBackEdge;
        }
    }
}
