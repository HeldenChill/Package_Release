using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Gameplay.Character.BrainSystem;

namespace Gameplay.Character.EditorTools
{
    public sealed class BrainGraphEditorWindow : EditorWindow
    {
        private const float ToolbarHeight = 28f;
        private const float InspectorWidth = 320f;
        private static readonly Color EdgeColor = new Color(0.55f, 0.67f, 0.78f, 1f);
        private static readonly Color RunningColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        private static readonly Color SuccessColor = new Color(0.2f, 0.75f, 1f, 1f);
        private static readonly Color FailureColor = new Color(1f, 0.35f, 0.35f, 1f);

        [SerializeField] private BrainGraphAsset graph;
        [SerializeField] private Vector2 pan;
        [SerializeField] private float zoom = 1f;
        [SerializeField] private bool followRuntime = true;
        [SerializeField] private bool autoFocusRuntime = true;

        private BrainGraphNode selectedNode;
        private BrainGraphEdge selectedEdge;
        private int connectFromNodeId = -1;
        private bool isPanning;
        private Vector2 lastMouse;
        private BrainGraphNode draggingNode;
        private Vector2 dragOffset;
        private int hoverPortNode = -1;
        private bool hoverPortInput;
        private readonly Dictionary<int, Rect> nodeRects = new();
        private readonly List<BrainGraphEdge> edgeSortBuffer = new();

        [MenuItem("Tools/Character/BrainGraph Editor")]
        public static void OpenMenu()
        {
            Open();
        }

        public static BrainGraphEditorWindow Open()
        {
            return GetWindow<BrainGraphEditorWindow>("BrainGraph");
        }

        public static BrainGraphEditorWindow Open(BrainGraphAsset asset)
        {
            BrainGraphEditorWindow window = GetWindow<BrainGraphEditorWindow>("BrainGraph");
            window.graph = asset;
            window.Repaint();
            return window;
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            // Intercept Delete/Backspace BEFORE the toolbar's ObjectField sees it — otherwise the
            // focused ObjectField consumes Delete to clear the assigned graph asset.
            HandleDeleteKey();

            DrawToolbar();
            if (graph == null)
            {
                DrawEmptyState();
                return;
            }

            graph.EnsureValidGraph();
            BrainGraphModule runtime = BrainGraphRuntimeRegistry.FindFirstByGraph(graph);
            if (followRuntime && autoFocusRuntime && Application.isPlaying && runtime != null && runtime.DebugState != null)
            {
                int focusId = runtime.DebugState.activeLeafId >= 0 ? runtime.DebugState.activeLeafId : runtime.DebugState.currentNodeId;
                if (focusId >= 0) FocusNodeSoft(focusId);
            }

            Rect graphRect = new Rect(0, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight);
            Rect inspectorRect = new Rect(position.width - InspectorWidth, ToolbarHeight, InspectorWidth, position.height - ToolbarHeight);

            DrawGrid(graphRect, 20f * zoom, new Color(1f, 1f, 1f, 0.07f));
            DrawGrid(graphRect, 100f * zoom, new Color(1f, 1f, 1f, 0.12f));
            HandleGraphEvents(graphRect);
            DrawEdges(graphRect, runtime);
            DrawNodes(graphRect, runtime);
            DrawConnectionPreview(graphRect);
            DrawInspector(inspectorRect, runtime);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(graph);
            }

            if (Application.isPlaying && followRuntime)
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight)))
            {
                graph = (BrainGraphAsset)EditorGUILayout.ObjectField(graph, typeof(BrainGraphAsset), false, GUILayout.Width(280));
                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(48))) CreateNewGraphAsset();
                GUI.enabled = graph != null;
                if (GUILayout.Button("Default Enemy", EditorStyles.toolbarButton, GUILayout.Width(100))) BuildDefaultEnemyGraph();
                if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(86))) AutoLayout();
                if (GUILayout.Button("Frame", EditorStyles.toolbarButton, GUILayout.Width(52))) FrameAll();
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(48))) Save();
                GUILayout.Space(8);
                followRuntime = GUILayout.Toggle(followRuntime, "Follow Run", EditorStyles.toolbarButton, GUILayout.Width(84));
                autoFocusRuntime = GUILayout.Toggle(autoFocusRuntime, "Auto Focus", EditorStyles.toolbarButton, GUILayout.Width(84));
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Zoom {zoom:0.00}x", EditorStyles.miniLabel, GUILayout.Width(72));
                GUI.enabled = true;
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.Space(90);
            EditorGUILayout.HelpBox("Select or create a BrainGraphAsset. The graph is a visual/runtime replacement for Unity Behavior Graph: nodes read BrainParameter and write BrainData only.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create BrainGraph Asset", GUILayout.Width(220), GUILayout.Height(32))) CreateNewGraphAsset();
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawGrid(Rect rect, float spacing, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Handles.BeginGUI();
            Handles.color = color;
            float xOffset = pan.x % spacing;
            float yOffset = pan.y % spacing;
            for (float x = rect.x + xOffset; x < rect.xMax; x += spacing)
            {
                Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
            }
            for (float y = rect.y + yOffset; y < rect.yMax; y += spacing)
            {
                Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
            }
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes(Rect graphRect, BrainGraphModule runtime)
        {
            nodeRects.Clear();
            GUI.BeginClip(graphRect);
            Vector2 clipOffset = graphRect.position;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                BrainGraphNode node = graph.nodes[i];
                Rect screenRect = WorldToScreen(new Rect(node.editorPosition, node.editorSize));
                nodeRects[node.id] = screenRect;                       // window-space for events/edges
                DrawNodeBox(new Rect(screenRect.position - clipOffset, screenRect.size), node, runtime);
            }
            GUI.EndClip();
        }

        // Plain-box node draw (no GUI.Window) so node drag + drag-to-connect are handled manually
        // in HandleGraphEvents. rect is clip-local (graph area origin).
        private void DrawNodeBox(Rect rect, BrainGraphNode node, BrainGraphModule runtime)
        {
            bool isActive = runtime != null && runtime.DebugState != null && runtime.DebugState.activeLeafId == node.id;

            // Active (currently running) node gets a bright glow ring behind the box so it's obvious
            // even at low zoom — the old thin green border was easy to miss.
            if (isActive)
            {
                Rect glow = new Rect(rect.x - 6, rect.y - 6, rect.width + 12, rect.height + 12);
                EditorGUI.DrawRect(glow, new Color(0.35f, 0.85f, 0.45f, 0.35f));
            }

            EditorGUI.DrawRect(rect, isActive ? new Color(0.18f, 0.30f, 0.20f, 0.98f) : new Color(0.16f, 0.16f, 0.18f, 0.96f));
            Color tint = GetNodeTint(node, runtime);
            // Border priority: active running > selected > tint. Active uses a thick bright-green ring.
            if (isActive)
                DrawBorder(rect, RunningColor, 4f);
            else
                DrawBorder(rect, selectedNode == node ? new Color(1f, 0.9f, 0.4f, 1f) : tint, selectedNode == node ? 3f : 1.5f);

            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 24f);
            EditorGUI.DrawRect(titleRect, GetKindColor(node.kind));
            GUI.Label(new Rect(rect.x + 8, rect.y + 3, rect.width - 16, 18), node.title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 8, rect.y + 28, rect.width - 16, 16), node.kind.ToString(), EditorStyles.miniBoldLabel);
            if (!string.IsNullOrWhiteSpace(node.note))
                GUI.Label(new Rect(rect.x + 8, rect.y + 46, rect.width - 16, rect.height - 50), node.note, EditorStyles.wordWrappedMiniLabel);

            // Ports are invisible full-width hover zones (Unity Behavior style). No drawn box.
            // On hover (or while wiring), a thin highlight line appears just inside the edge.
            bool hoverIn = hoverPortNode == node.id && hoverPortInput;
            bool hoverOut = hoverPortNode == node.id && !hoverPortInput;
            bool wiringTarget = connectFromNodeId >= 0 && connectFromNodeId != node.id;
            if (hoverIn || wiringTarget)
            {
                Color c = wiringTarget ? new Color(0.4f, 0.95f, 0.5f, 1f) : new Color(0.8f, 0.9f, 1f, 0.9f);
                EditorGUI.DrawRect(new Rect(rect.x + 6, rect.y + 2f, rect.width - 12, 3f), c);
            }
            if (hoverOut || connectFromNodeId == node.id)
            {
                Color c = connectFromNodeId == node.id ? Color.yellow : new Color(0.8f, 0.9f, 1f, 0.9f);
                EditorGUI.DrawRect(new Rect(rect.x + 6, rect.yMax - 5f, rect.width - 12, 3f), c);
            }
        }

        private static void DrawBorder(Rect r, Color c, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        private void DrawEdges(Rect graphRect, BrainGraphModule runtime)
        {
            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.MouseDown) return;
            Handles.BeginGUI();
            edgeSortBuffer.Clear();
            edgeSortBuffer.AddRange(graph.edges);
            edgeSortBuffer.Sort((a, b) => a.order.CompareTo(b.order));

            // Clip edges to the graph area so they never paint over the inspector panel.
            // Handles draw in clip-local space, so offset all points by -graphRect.position.
            Vector2 off = graphRect.position;
            GUI.BeginClip(graphRect);
            for (int i = 0; i < edgeSortBuffer.Count; i++)
            {
                BrainGraphEdge edge = edgeSortBuffer[i];
                if (edge == null || edge.disabled) continue;
                if (!nodeRects.TryGetValue(edge.fromNodeId, out Rect fromRect)) continue;
                if (!nodeRects.TryGetValue(edge.toNodeId, out Rect toRect)) continue;

                Vector2 start = GetOutputPortRect(fromRect).center - off;
                Vector2 end = GetInputPortRect(toRect).center - off;

                Color color = EdgeColor;
                float width = 3.5f;
                if (runtime != null && runtime.Runner != null)
                {
                    bool active = runtime.Runner.VisitedThisTick.Contains(edge.fromNodeId) && runtime.Runner.VisitedThisTick.Contains(edge.toNodeId);
                    if (active)
                    {
                        color = RunningColor;
                        width = 6f;
                    }
                }
                if (selectedEdge == edge)
                {
                    color = Color.yellow;
                    width = 6f;
                }

                DrawStepEdge(start, end, color, width);
                DrawArrow(end, Vector2.down, color, width);

                if (!string.IsNullOrEmpty(edge.label))
                {
                    Vector2 mid = Vector2.Lerp(start, end, 0.5f);
                    GUI.Label(new Rect(mid.x - 50, mid.y - 12, 100, 20), edge.label, EditorStyles.miniLabel);
                }
            }
            GUI.EndClip();
            Handles.EndGUI();
        }

        private void DrawArrow(Vector2 tip, Vector2 dir, Color color, float width)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            float size = Mathf.Max(7f, width * 1.8f);
            Vector2 baseP = tip - dir * (size * 0.9f);
            Vector2 left = Quaternion.Euler(0, 0, 150) * dir;
            Vector2 right = Quaternion.Euler(0, 0, -150) * dir;
            Handles.color = color;
            Handles.DrawAAConvexPolygon(tip, baseP + left * size, baseP + right * size);
        }

        // Behavior-style orthogonal connector: down from output, horizontal across, down into input,
        // with small rounded elbows. No bezier.
        private void DrawStepEdge(Vector2 start, Vector2 end, Color color, float width)
        {
            Handles.color = color;
            float midY = (start.y + end.y) * 0.5f;
            // If the target is above (back-edge), push the mid band below the start so it routes cleanly.
            if (end.y <= start.y + 24f) midY = start.y + 28f;

            float r = Mathf.Min(10f, Mathf.Abs(end.x - start.x) * 0.5f, Mathf.Abs(midY - start.y));
            int dirX = end.x >= start.x ? 1 : -1;

            Vector2 p0 = start;
            Vector2 p1 = new Vector2(start.x, midY);          // down
            Vector2 p2 = new Vector2(end.x, midY);            // across
            Vector2 p3 = end;                                 // down into input

            if (r < 2f)
            {
                DrawThickPolyline(width, color, p0, p1, p2, p3);
                return;
            }

            // Segments with rounded corners at the two elbows.
            Vector2 a1 = new Vector2(p1.x, p1.y - r);
            Vector2 a2 = new Vector2(p1.x + dirX * r, p1.y);
            Vector2 b1 = new Vector2(p2.x - dirX * r, p2.y);
            Vector2 b2 = new Vector2(p2.x, p2.y + r);

            DrawThickLine(p0, a1, width, color);
            DrawCorner(a1, p1, a2, width, color);
            DrawThickLine(a2, b1, width, color);
            DrawCorner(b1, p2, b2, width, color);
            DrawThickLine(b2, p3, width, color);
        }

        private static void DrawThickLine(Vector2 a, Vector2 b, float width, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(width, a, b);
        }

        private static void DrawThickPolyline(float width, Color color, params Vector2[] pts)
        {
            Handles.color = color;
            Vector3[] v = new Vector3[pts.Length];
            for (int i = 0; i < pts.Length; i++) v[i] = pts[i];
            Handles.DrawAAPolyLine(width, v);
        }

        private static void DrawCorner(Vector2 from, Vector2 corner, Vector2 to, float width, Color color)
        {
            Handles.color = color;
            const int seg = 6;
            Vector3[] v = new Vector3[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                // Quadratic bezier through the corner control point = smooth elbow.
                Vector2 m1 = Vector2.Lerp(from, corner, t);
                Vector2 m2 = Vector2.Lerp(corner, to, t);
                v[i] = Vector2.Lerp(m1, m2, t);
            }
            Handles.DrawAAPolyLine(width, v);
        }

        private void DrawConnectionPreview(Rect graphRect)
        {
            if (connectFromNodeId < 0) return;
            if (!nodeRects.TryGetValue(connectFromNodeId, out Rect fromRect)) return;
            Handles.BeginGUI();
            Vector2 start = GetOutputPortRect(fromRect).center;
            Vector2 end = Event.current.mousePosition;
            DrawStepEdge(start, end, Color.yellow, 4f);
            Handles.EndGUI();
            Repaint();
        }

        private void DrawInspector(Rect rect, BrainGraphModule runtime)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            EditorGUILayout.LabelField("BrainGraph Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (runtime != null && runtime.DebugState != null)
            {
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Module", runtime.name);
                EditorGUILayout.LabelField("Active Leaf", runtime.DebugState.activeLeafId.ToString());
                EditorGUILayout.LabelField("Current Node", runtime.DebugState.currentNodeId.ToString());
                EditorGUILayout.LabelField("Last Status", runtime.DebugState.lastStatus.ToString());
                EditorGUILayout.LabelField("Message", runtime.DebugState.lastMessage ?? string.Empty, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(8);
            }

            if (selectedNode != null)
            {
                DrawNodeInspector(selectedNode);
            }
            else if (selectedEdge != null)
            {
                DrawEdgeInspector(selectedEdge);
            }
            else
            {
                DrawGraphInspector();
            }
            GUILayout.EndArea();
        }

        private void DrawGraphInspector()
        {
            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            graph.startNodeId = EditorGUILayout.IntField("Start Node Id", graph.startNodeId);
            graph.resetBrainDataBeforeTick = EditorGUILayout.Toggle("Reset Data Before Tick", graph.resetBrainDataBeforeTick);
            graph.seenMemorySeconds = EditorGUILayout.FloatField("Seen Memory Seconds", graph.seenMemorySeconds);
            graph.soundMemorySeconds = EditorGUILayout.FloatField("Sound Memory Seconds", graph.soundMemorySeconds);
            graph.worldChangeMemorySeconds = EditorGUILayout.FloatField("World Change Memory Seconds", graph.worldChangeMemorySeconds);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Nodes: {graph.nodes.Count}  Edges: {graph.edges.Count}", EditorStyles.miniLabel);
        }

        private void DrawNodeInspector(BrainGraphNode node)
        {
            EditorGUILayout.LabelField("Node", EditorStyles.boldLabel);
            node.title = EditorGUILayout.TextField("Title", node.title);
            node.kind = (BrainGraphNodeKind)EditorGUILayout.EnumPopup("Kind", node.kind);
            node.disabled = EditorGUILayout.Toggle("Disabled", node.disabled);
            node.note = EditorGUILayout.TextArea(node.note, GUILayout.MinHeight(46));
            EditorGUILayout.Space(6);

            if (NeedsTargetSource(node.kind)) node.targetSource = (BrainGraphTargetSource)EditorGUILayout.EnumPopup("Target Source", node.targetSource);
            if (NeedsCondition(node.kind))
            {
                node.condition = (BrainGraphConditionKind)EditorGUILayout.EnumPopup("Condition", node.condition);
                node.invertCondition = EditorGUILayout.Toggle("Invert", node.invertCondition);
            }
            if (NeedsVector(node.kind, node.targetSource)) node.vectorValue = EditorGUILayout.Vector3Field("Vector Value", node.vectorValue);
            if (NeedsFloat(node.kind)) node.floatValue = EditorGUILayout.FloatField("Float Value", node.floatValue);
            if (NeedsInt(node.kind)) node.intValue = EditorGUILayout.IntField("Int Value", node.intValue);
            if (node.kind == BrainGraphNodeKind.DebugLog) node.stringValue = EditorGUILayout.TextField("Message", node.stringValue);

            if (node.kind == BrainGraphNodeKind.SetBrainFlag)
            {
                node.writeMoveTarget = EditorGUILayout.Toggle("Write Move Target", node.writeMoveTarget);
                node.writeSeePosition = EditorGUILayout.Toggle("Write See Position", node.writeSeePosition);
                node.writeRun = EditorGUILayout.Toggle("Write Run", node.writeRun);
                node.writeCrouch = EditorGUILayout.Toggle("Write Crouch", node.writeCrouch);
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Set Start")) graph.startNodeId = node.id;
                if (GUILayout.Button("Focus")) FocusNode(node.id);
                if (GUILayout.Button("Open Script")) OpenNodeScript(node.kind);
            }
            if (GUILayout.Button("Delete Node"))
            {
                Undo.RecordObject(graph, "Delete BrainGraph Node");
                graph.RemoveNode(node.id);
                selectedNode = null;
                Save();
            }
        }

        private void DrawEdgeInspector(BrainGraphEdge edge)
        {
            EditorGUILayout.LabelField("Edge", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("From", edge.fromNodeId.ToString());
            EditorGUILayout.LabelField("To", edge.toNodeId.ToString());
            edge.order = EditorGUILayout.IntField("Order", edge.order);
            edge.label = EditorGUILayout.TextField("Label", edge.label);
            edge.disabled = EditorGUILayout.Toggle("Disabled", edge.disabled);
            if (GUILayout.Button("Delete Edge"))
            {
                Undo.RecordObject(graph, "Delete BrainGraph Edge");
                graph.RemoveEdge(edge);
                selectedEdge = null;
                Save();
            }
        }

        private static bool NeedsTargetSource(BrainGraphNodeKind kind)
        {
            return kind == BrainGraphNodeKind.MoveToTarget || kind == BrainGraphNodeKind.KeepTrackOfPosition || kind == BrainGraphNodeKind.LookAroundPosition;
        }

        private static bool NeedsCondition(BrainGraphNodeKind kind) => kind == BrainGraphNodeKind.Condition;
        private static bool NeedsVector(BrainGraphNodeKind kind, BrainGraphTargetSource source) => kind == BrainGraphNodeKind.SetBrainFlag || source == BrainGraphTargetSource.ManualPosition;
        private static bool NeedsFloat(BrainGraphNodeKind kind) => kind == BrainGraphNodeKind.SeeSomething || kind == BrainGraphNodeKind.HearSomething || kind == BrainGraphNodeKind.DetectWorldStateChange || kind == BrainGraphNodeKind.LookAround || kind == BrainGraphNodeKind.LookAroundPosition || kind == BrainGraphNodeKind.MoveToTarget;
        private static bool NeedsInt(BrainGraphNodeKind kind) => kind == BrainGraphNodeKind.Condition || kind == BrainGraphNodeKind.SetBrainFlag;

        private Color GetNodeTint(BrainGraphNode node, BrainGraphModule runtime)
        {
            if (selectedNode == node) return new Color(1f, 0.93f, 0.55f, 1f);
            if (runtime != null && runtime.Runner != null)
            {
                if (runtime.DebugState != null && runtime.DebugState.activeLeafId == node.id) return RunningColor;
                if (runtime.Runner.VisitedThisTick.Contains(node.id)) return SuccessColor;
                if (runtime.Runner.VisitedEver.Contains(node.id)) return new Color(0.78f, 0.86f, 1f, 1f);
            }
            return node.disabled ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
        }

        private Color GetKindColor(BrainGraphNodeKind kind)
        {
            switch (kind)
            {
                case BrainGraphNodeKind.Root: return new Color(0.2f, 0.4f, 0.8f, 1f);
                case BrainGraphNodeKind.Sequence:
                case BrainGraphNodeKind.Selector:
                case BrainGraphNodeKind.Parallel:
                    return new Color(0.45f, 0.35f, 0.75f, 1f);
                case BrainGraphNodeKind.Perception:
                case BrainGraphNodeKind.SeeSomething:
                case BrainGraphNodeKind.HearSomething:
                    return new Color(0.2f, 0.55f, 0.65f, 1f);
                case BrainGraphNodeKind.MoveToTarget:
                    return new Color(0.35f, 0.55f, 0.25f, 1f);
                case BrainGraphNodeKind.Condition:
                    return new Color(0.75f, 0.55f, 0.15f, 1f);
                default:
                    return new Color(0.35f, 0.35f, 0.35f, 1f);
            }
        }

        private Rect WorldToScreen(Rect world)
        {
            return new Rect(world.position * zoom + pan + new Vector2(0, ToolbarHeight), world.size * zoom);
        }

        private Vector2 ScreenToWorld(Vector2 screen)
        {
            return (screen - pan - new Vector2(0, ToolbarHeight)) / zoom;
        }

        private const float PortBand = 12f; // thickness of the invisible full-width hover/connect band
        // Vertical flow: input band along the top edge, output band along the bottom edge,
        // spanning the full node width and bulging slightly outside (up for input, down for output).
        // Edge anchor = band center (node center.x at the edge), via .center.
        private static Rect GetInputPortRect(Rect nodeRect) => new Rect(nodeRect.x, nodeRect.y - PortBand * 0.6f, nodeRect.width, PortBand);
        private static Rect GetOutputPortRect(Rect nodeRect) => new Rect(nodeRect.x, nodeRect.yMax - PortBand * 0.4f, nodeRect.width, PortBand);

        // Runs before the toolbar draws so the ObjectField never gets the Delete key (which would
        // otherwise clear the assigned graph asset). Deletes the selected edge, else selected node.
        private void HandleDeleteKey()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode != KeyCode.Delete && e.keyCode != KeyCode.Backspace) return;
            if (graph == null) return;

            if (selectedEdge != null)
            {
                Undo.RecordObject(graph, "Delete BrainGraph Edge");
                graph.RemoveEdge(selectedEdge);
                selectedEdge = null;
                GUIUtility.keyboardControl = 0;
                Save();
                e.Use();
                return;
            }
            if (selectedNode != null)
            {
                Undo.RecordObject(graph, "Delete BrainGraph Node");
                graph.RemoveNode(selectedNode.id);
                selectedNode = null;
                GUIUtility.keyboardControl = 0;
                Save();
                e.Use();
            }
        }

        private void HandleGraphEvents(Rect graphRect)
        {
            Event e = Event.current;

            if (!graphRect.Contains(e.mousePosition)) { hoverPortNode = -1; return; }

            // Track which port band the mouse is over (for the hover highlight line).
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                int prevNode = hoverPortNode; bool prevIn = hoverPortInput;
                hoverPortNode = -1;
                int o = FindPortNode(e.mousePosition, output: true);
                if (o >= 0) { hoverPortNode = o; hoverPortInput = false; }
                else { int ip = FindPortNode(e.mousePosition, output: false); if (ip >= 0) { hoverPortNode = ip; hoverPortInput = true; } }
                if (prevNode != hoverPortNode || prevIn != hoverPortInput) Repaint();
            }

            if (e.type == EventType.ScrollWheel)
            {
                float oldZoom = zoom;
                zoom = Mathf.Clamp(zoom - e.delta.y * 0.03f, 0.35f, 1.5f);
                Vector2 mouseWorldBefore = (e.mousePosition - pan - new Vector2(0, ToolbarHeight)) / oldZoom;
                pan = e.mousePosition - new Vector2(0, ToolbarHeight) - mouseWorldBefore * zoom;
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 2)
            {
                isPanning = true;
                lastMouse = e.mousePosition;
                e.Use();
            }
            if (e.type == EventType.MouseDrag && isPanning)
            {
                pan += e.mousePosition - lastMouse;
                lastMouse = e.mousePosition;
                e.Use();
            }
            if (e.type == EventType.MouseUp && e.button == 2)
            {
                isPanning = false;
                e.Use();
            }

            if (e.type == EventType.ContextClick)
            {
                ShowContextMenu(e.mousePosition);
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // 1) Output port pressed -> begin a drag-to-connect wire (shader-graph style).
                int outPort = FindPortNode(e.mousePosition, output: true);
                if (outPort >= 0)
                {
                    connectFromNodeId = outPort;
                    e.Use();
                    return;
                }
                // 2) Input port pressed with an existing edge -> unplug and carry the wire.
                int inPort = FindPortNode(e.mousePosition, output: false);
                if (inPort >= 0)
                {
                    BrainGraphEdge incoming = graph.edges.FirstOrDefault(x => x != null && x.toNodeId == inPort);
                    if (incoming != null)
                    {
                        Undo.RecordObject(graph, "Unplug BrainGraph Edge");
                        int from = incoming.fromNodeId;
                        graph.RemoveEdge(incoming);
                        connectFromNodeId = from;
                        selectedEdge = null;
                        Save();
                        e.Use();
                        return;
                    }
                }
                // 3) Node body pressed -> select + begin node drag.
                BrainGraphNode hit = HitNode(e.mousePosition);
                if (hit != null)
                {
                    selectedNode = hit;
                    selectedEdge = null;
                    draggingNode = hit;
                    dragOffset = e.mousePosition - WorldToScreen(new Rect(hit.editorPosition, hit.editorSize)).position;
                    e.Use();
                    return;
                }
                // 4) Edge or empty.
                if (TrySelectEdge(e.mousePosition)) { e.Use(); return; }
                selectedNode = null;
                selectedEdge = null;
                connectFromNodeId = -1;
            }

            // Node drag move.
            if (e.type == EventType.MouseDrag && e.button == 0 && draggingNode != null)
            {
                Undo.RecordObject(graph, "Move BrainGraph Node");
                draggingNode.editorPosition = ScreenToWorld(e.mousePosition - dragOffset);
                e.Use();
            }

            // Mouse up: finish a connect drag (release on input port) or end node drag.
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (connectFromNodeId >= 0)
                {
                    int target = FindPortNode(e.mousePosition, output: false);
                    if (target < 0) { BrainGraphNode n = HitNode(e.mousePosition); if (n != null) target = n.id; }
                    if (target >= 0 && target != connectFromNodeId)
                    {
                        Undo.RecordObject(graph, "Connect BrainGraph Nodes");
                        graph.AddEdge(connectFromNodeId, target);
                        Save();
                    }
                    connectFromNodeId = -1;
                    e.Use();
                }
                if (draggingNode != null)
                {
                    draggingNode = null;
                    Save();
                    e.Use();
                }
            }

            // Repaint live while dragging a wire so the preview follows the cursor.
            if (connectFromNodeId >= 0 && e.type == EventType.MouseDrag) Repaint();
        }

        private int FindPortNode(Vector2 mouse, bool output)
        {
            foreach (var kv in nodeRects)
            {
                Rect port = output ? GetOutputPortRect(kv.Value) : GetInputPortRect(kv.Value);
                if (port.Contains(mouse)) return kv.Key;
            }
            return -1;
        }

        private BrainGraphNode HitNode(Vector2 mouse)
        {
            for (int i = graph.nodes.Count - 1; i >= 0; i--)
            {
                BrainGraphNode node = graph.nodes[i];
                if (nodeRects.TryGetValue(node.id, out Rect rect) && rect.Contains(mouse)) return node;
            }
            return null;
        }


        private bool TrySelectEdge(Vector2 mouse)
        {
            foreach (BrainGraphEdge edge in graph.edges)
            {
                if (!nodeRects.TryGetValue(edge.fromNodeId, out Rect fromRect)) continue;
                if (!nodeRects.TryGetValue(edge.toNodeId, out Rect toRect)) continue;
                Vector2 a = GetOutputPortRect(fromRect).center;
                Vector2 b = GetInputPortRect(toRect).center;
                // Match the step shape: down to midY, across, down into input.
                float midY = (a.y + b.y) * 0.5f;
                if (b.y <= a.y + 24f) midY = a.y + 28f;
                Vector2 m1 = new Vector2(a.x, midY);
                Vector2 m2 = new Vector2(b.x, midY);
                float distance = Mathf.Min(
                    DistPointSeg(mouse, a, m1),
                    Mathf.Min(DistPointSeg(mouse, m1, m2), DistPointSeg(mouse, m2, b)));
                if (distance <= 8f)
                {
                    selectedEdge = edge;
                    selectedNode = null;
                    return true;
                }
            }
            return false;
        }

        private static float DistPointSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        // Behavior-Graph-style grouped create menu: nodes are filed under their category by enum
        // band (Flow 0-99, Perception 100-199, Action 200-299, Logic 300-399, Debug 900+). Band is
        // the single source of truth so codegen'd custom nodes auto-categorize from their enum value.
        private static string CategoryOf(BrainGraphNodeKind kind)
        {
            int v = (int)kind;
            if (v < 100) return "Flow";
            if (v < 200) return "Perception";
            if (v < 300) return "Action";
            if (v < 400) return "Logic";
            return "Debug";
        }

        private static bool IsActionKind(BrainGraphNodeKind kind) => CategoryOf(kind) == "Action";

        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu menu = new GenericMenu();
            Vector2 world = ScreenToWorld(mousePosition);
            foreach (BrainGraphNodeKind kind in Enum.GetValues(typeof(BrainGraphNodeKind)))
            {
                BrainGraphNodeKind captured = kind;
                menu.AddItem(new GUIContent($"Create/{CategoryOf(kind)}/{kind}"), false, () => AddNode(captured, world));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("New Node Type..."), false, BrainGraphNewNodePopup.Show);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Frame All"), false, FrameAll);
            menu.ShowAsContext();
        }

        private void AddNode(BrainGraphNodeKind kind, Vector2 worldPosition)
        {
            Undo.RecordObject(graph, "Add BrainGraph Node");
            BrainGraphNode node = graph.AddNode(kind, worldPosition);
            selectedNode = node;
            selectedEdge = null;
            // Action nodes prompt for a name on create (Behavior-Graph style).
            if (IsActionKind(kind))
                BrainGraphNameNodePopup.Show(this, node, kind.ToString());
            Save();
        }

        private void BuildDefaultEnemyGraph()
        {
            if (!EditorUtility.DisplayDialog("Build default enemy graph", "This will replace the current graph nodes and edges.", "Replace", "Cancel")) return;
            Undo.RecordObject(graph, "Build Default BrainGraph");
            graph.BuildDefaultEnemyGraph();
            selectedNode = null;
            selectedEdge = null;
            Save();
            FrameAll();
        }

        private const float LayoutHGap = 40f;   // horizontal gap between sibling subtrees
        private const float LayoutVGap = 90f;    // vertical gap between depth layers

        private void AutoLayout()
        {
            if (graph == null || graph.nodes.Count == 0) return;
            Undo.RecordObject(graph, "Auto Layout BrainGraph");

            int start = graph.startNodeId >= 0 && graph.GetNode(graph.startNodeId) != null ? graph.startNodeId : graph.nodes[0].id;
            HashSet<int> visited = new();
            float cursorX = 80f;
            LayoutSubtree(start, 0, ref cursorX, visited);

            // Any nodes not reachable from start: drop them in a row below everything.
            float orphanY = 80f;
            foreach (BrainGraphNode n in graph.nodes)
                if (depthLayout.TryGetValue(n.id, out int d)) orphanY = Mathf.Max(orphanY, n.editorPosition.y + n.editorSize.y);
            float orphanX = 80f;
            foreach (BrainGraphNode n in graph.nodes)
            {
                if (visited.Contains(n.id)) continue;
                n.editorPosition = new Vector2(orphanX, orphanY + 120f);
                orphanX += n.editorSize.x + LayoutHGap;
            }

            depthLayout.Clear();
            Save();
            FrameAll();
        }

        private readonly Dictionary<int, int> depthLayout = new();

        // Returns the horizontal center x assigned to this subtree's root. Parent sits centered
        // above its children; children are laid out left-to-right with no overlap (Behavior-style).
        private float LayoutSubtree(int nodeId, int depth, ref float cursorX, HashSet<int> visited)
        {
            BrainGraphNode node = graph.GetNode(nodeId);
            if (node == null || !visited.Add(nodeId)) return cursorX;
            depthLayout[nodeId] = depth;
            float y = 80f + depth * (node.editorSize.y + LayoutVGap);

            graph.GetOutgoing(nodeId, edgeSortBuffer);
            List<int> children = new();
            foreach (BrainGraphEdge e in edgeSortBuffer)
                if (e != null && !e.disabled && !visited.Contains(e.toNodeId)) children.Add(e.toNodeId);

            float centerX;
            if (children.Count == 0)
            {
                centerX = cursorX + node.editorSize.x * 0.5f;
                cursorX += node.editorSize.x + LayoutHGap;
            }
            else
            {
                float first = 0f, last = 0f;
                for (int i = 0; i < children.Count; i++)
                {
                    float c = LayoutSubtree(children[i], depth + 1, ref cursorX, visited);
                    if (i == 0) first = c;
                    last = c;
                }
                centerX = (first + last) * 0.5f;
            }

            node.editorPosition = new Vector2(centerX - node.editorSize.x * 0.5f, y);
            return centerX;
        }

        private void FrameAll()
        {
            if (graph == null || graph.nodes.Count == 0) return;
            Rect bounds = new Rect(graph.nodes[0].editorPosition, graph.nodes[0].editorSize);
            foreach (BrainGraphNode node in graph.nodes)
            {
                bounds = Union(bounds, new Rect(node.editorPosition, node.editorSize));
            }
            Rect graphRect = new Rect(0, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight);
            zoom = Mathf.Clamp(Mathf.Min(graphRect.width / (bounds.width + 200f), graphRect.height / (bounds.height + 200f)), 0.35f, 1.25f);
            pan = graphRect.center - bounds.center * zoom - new Vector2(0, ToolbarHeight);
            Repaint();
        }

        private void FocusNode(int nodeId)
        {
            BrainGraphNode node = graph.GetNode(nodeId);
            if (node == null) return;
            Rect graphRect = new Rect(0, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight);
            pan = graphRect.center - (node.editorPosition + node.editorSize * 0.5f) * zoom - new Vector2(0, ToolbarHeight);
            Repaint();
        }

        private void FocusNodeSoft(int nodeId)
        {
            BrainGraphNode node = graph.GetNode(nodeId);
            if (node == null) return;
            Vector2 desired = new Rect(0, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight).center - (node.editorPosition + node.editorSize * 0.5f) * zoom - new Vector2(0, ToolbarHeight);
            pan = Vector2.Lerp(pan, desired, 0.04f);
        }

        private static Rect Union(Rect a, Rect b)
        {
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void CreateNewGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create BrainGraph", "EnemyBrainGraph", "asset", "Choose a location for the BrainGraph asset.");
            if (string.IsNullOrEmpty(path)) return;
            BrainGraphAsset asset = CreateInstance<BrainGraphAsset>();
            asset.BuildDefaultEnemyGraph();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            graph = asset;
            FrameAll();
        }

        // Jump to the C# that implements this node kind. Each leaf kind is its own file <Kind>Node.cs
        // (IBrainGraphNode). Composites (Root/Sequence/Selector/Parallel/Comment) have no handler
        // file — they are evaluated in BrainGraphRunner, so open that instead.
        private static void OpenNodeScript(BrainGraphNodeKind kind)
        {
            bool composite = kind == BrainGraphNodeKind.Root || kind == BrainGraphNodeKind.Sequence
                || kind == BrainGraphNodeKind.Selector || kind == BrainGraphNodeKind.Parallel
                || kind == BrainGraphNodeKind.Comment;
            string fileName = composite ? "BrainGraphRunner" : kind + "Node";

            string guid = AssetDatabase.FindAssets($"{fileName} t:MonoScript").FirstOrDefault();
            if (guid == null)
            {
                Debug.LogWarning($"BrainGraph: could not find script {fileName}.cs");
                return;
            }
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        private void Save()
        {
            if (graph == null) return;
            graph.EnsureValidGraph();
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }
}
