using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Graph editor and live debugger for AIGraphAsset.</summary>
    public sealed class AIGraphEditorWindow : EditorWindow
    {
        [SerializeField] AIGraphAsset asset;
        [SerializeField] AIAgent debugAgent;
        [SerializeField] bool showMiniMap = true;
        AIGraphView graphView;
        AIBlackboardPanel blackboardPanel;
        AINodeInspectorPanel inspectorPanel;
        AIGraphDebugger debugger;
        AIConsolePanel consolePanel;
        ObjectField graphField;
        ObjectField agentField;
        Label statusLabel;
        Button playButton;
        Button stepButton;
        Button moreButton;
        string selectedNodeId;

        public AIGraphView GraphView => graphView;

        public AIGraphAsset Asset => asset;

        public AIGraphDebugger Debugger => debugger;

        public ObjectField GraphField => graphField;

        [MenuItem("Tools/Character/AI Graph Editor")]
        public static void ShowWindow()
        {
            GetWindow<AIGraphEditorWindow>("AI Graph");
        }

        public static AIGraphEditorWindow Open(AIGraphAsset target, AIAgent agent = null)
        {
            var window = GetWindow<AIGraphEditorWindow>("AI Graph");
            window.BuildGui();
            window.SetAsset(target);
            if (agent != null) window.SetAgent(agent);
            return window;
        }

        [OnOpenAsset]
        static bool OnOpenAsset(int instanceId, int line)
        {
            if (!(EditorUtility.InstanceIDToObject(instanceId) is AIGraphAsset graph)) return false;
            Open(graph);
            return true;
        }

        /// <summary>False while the key goes to a text field (sticky note, group title, inspector), so Space and F still type.</summary>
        public static bool ShouldHandleShortcut(VisualElement target) => target == null || target.GetFirstOfType<TextField>() == null;

        /// <summary>Opens another graph in this window. The toolbar Graph field calls this.</summary>
        public void SelectGraph(AIGraphAsset target)
        {
            if (target == asset)
            {
                graphField?.SetValueWithoutNotify(asset);
                return;
            }
            SetAsset(target);
        }

        /// <summary>Debugs this agent. An agent running a different graph opens that graph first; an agent with no graph keeps the open one.</summary>
        public void SetAgent(AIAgent agent)
        {
            if (agent != null && agent.Graph != null && agent.Graph != asset) SetAsset(agent.Graph);
            debugAgent = agent;
            agentField?.SetValueWithoutNotify(agent);
            debugger?.SetAgent(agent);
        }

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.pauseStateChanged += OnPauseChanged;
            // Re-enabled without a domain reload: the old UI is still attached but its graph view was disposed.
            if (rootVisualElement.childCount > 0) BuildGui();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.pauseStateChanged -= OnPauseChanged;
            debugger?.Unsubscribe();
            graphView?.Dispose();
            graphView = null;
        }

        void CreateGUI()
        {
            BuildGui();
        }

        void BuildGui()
        {
            if (graphView != null) return;
            VisualElement root = rootVisualElement;
            root.Clear();
            // Keep Unity's default editor theme and font stylesheets on the root.
            StyleSheet sheet = AIEditorUtil.LoadStyleSheet();
            if (sheet != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            root.AddToClassList("ai-window");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "ai-dark" : "ai-light");
            root.Add(BuildToolbar());
            var body = new VisualElement();
            body.AddToClassList("ai-body");
            root.Add(body);
            blackboardPanel = new AIBlackboardPanel(OnBlackboardChanged);
            graphView = new AIGraphView(this) { MiniMapVisible = showMiniMap };
            graphView.AddToClassList("ai-graph");
            inspectorPanel = new AINodeInspectorPanel(OnNodeEdited);
            body.Add(blackboardPanel);
            body.Add(graphView);
            body.Add(inspectorPanel);
            consolePanel = new AIConsolePanel();
            root.Add(consolePanel);
            graphView.NodeSelected += OnNodeSelected;
            graphView.GraphChanged += OnGraphChanged;
            graphView.RegisterCallback<KeyDownEvent>(OnGraphKeyDown, TrickleDown.TrickleDown);
            debugger = new AIGraphDebugger(graphView);
            consolePanel.SetLog(debugger.Log);
            graphView.schedule.Execute(UpdateDebugger).Every(100);
            Reload();
            graphField.SetValueWithoutNotify(asset);
            RefreshPlayButtons();
            if (debugAgent != null) SetAgent(debugAgent);
        }

        VisualElement BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("ai-toolbar");
            playButton = ToolbarButton(AIGlyphs.Play + " Play", TogglePlay, null);
            playButton.AddToClassList("ai-f-mono");
            stepButton = ToolbarButton("Step", Step, null);
            toolbar.Add(playButton);
            toolbar.Add(stepButton);
            toolbar.Add(Separator());
            toolbar.Add(ToolbarButton("+ Add node", OpenAddNode, "Space"));
            toolbar.Add(ToolbarButton("Frame all", () =>
            {
                if (graphView != null) graphView.FrameAll();
            }, "F"));
            toolbar.Add(ToolbarButton("Auto layout", AutoLayout, null));
            var spacer = new VisualElement();
            spacer.AddToClassList("ai-tb-spacer");
            toolbar.Add(spacer);
            graphField = new ObjectField("Graph") { objectType = typeof(AIGraphAsset), allowSceneObjects = false };
            graphField.RegisterValueChangedCallback(e => SelectGraph(e.newValue as AIGraphAsset));
            toolbar.Add(graphField);
            agentField = new ObjectField("Agent") { objectType = typeof(AIAgent), allowSceneObjects = true };
            agentField.RegisterValueChangedCallback(e => SetAgent(e.newValue as AIAgent));
            toolbar.Add(agentField);
            statusLabel = new Label();
            statusLabel.AddToClassList("ai-status");
            toolbar.Add(statusLabel);
            moreButton = ToolbarButton(AIGlyphs.More, ShowMoreMenu, null);
            moreButton.AddToClassList("ai-f-mono");
            moreButton.tooltip = "Save, minimap, remove missing nodes";
            toolbar.Add(moreButton);
            return toolbar;
        }

        static Button ToolbarButton(string text, Action click, string shortcut)
        {
            var button = new Button(click);
            button.AddToClassList("ai-tb-btn");
            var label = new Label(text);
            label.AddToClassList("ai-tb-text");
            button.Add(label);
            if (!string.IsNullOrEmpty(shortcut))
            {
                var key = new Label(shortcut);
                key.AddToClassList("ai-badge-key");
                button.Add(key);
            }
            return button;
        }

        static VisualElement Separator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("ai-tb-sep");
            return separator;
        }

        void SetAsset(AIGraphAsset target)
        {
            asset = target;
            selectedNodeId = null;
            titleContent = new GUIContent(asset != null ? asset.name : "AI Graph");
            graphField?.SetValueWithoutNotify(asset);
            Reload();
            if (asset != null) debugger?.Log.Info($"Graph loaded · {asset.Nodes.Count} nodes · {asset.Blackboard.Keys.Count} blackboard keys");
        }

        void Reload()
        {
            if (graphView == null) return;
            graphView.Load(asset);
            blackboardPanel.SetGraph(asset);
            if (selectedNodeId != null && (asset == null || asset.GetNode(selectedNodeId) == null)) selectedNodeId = null;
            // Load rebuilt every view, which dropped the selection; restore it so the graph matches the inspector.
            if (selectedNodeId != null && graphView.NodeViews.TryGetValue(selectedNodeId, out AINodeView view)) graphView.AddToSelection(view);
            ShowInspector(true);
            RefreshStatus();
        }

        void ShowInspector(bool force = false)
        {
            IReadOnlyList<string> issues = null;
            if (asset != null && selectedNodeId != null && AIGraphValidator.ValidateAll(asset).TryGetValue(selectedNodeId, out List<string> list)) issues = list;
            inspectorPanel.Show(asset, selectedNodeId, issues, force);
        }

        void RefreshStatus()
        {
            if (statusLabel == null) return;
            if (asset == null)
            {
                statusLabel.text = "Open an AI Graph asset.";
                statusLabel.tooltip = string.Empty;
                statusLabel.EnableInClassList("ai-status-error", false);
                return;
            }
            var errors = new List<string>();
            asset.Validate(errors);
            foreach (KeyValuePair<string, List<string>> pair in AIGraphValidator.ValidateAll(asset))
            {
                AINode node = asset.GetNode(pair.Key);
                string title = node != null ? node.Title : pair.Key;
                foreach (string issue in pair.Value)
                {
                    string line = $"{title}: {issue}";
                    if (!errors.Contains(line)) errors.Add(line);
                }
            }
            statusLabel.text = errors.Count == 0 ? "Valid" : errors.Count + " problem(s) - hover for details";
            statusLabel.tooltip = string.Join("\n", errors);
            statusLabel.EnableInClassList("ai-status-error", errors.Count > 0);
        }

        bool HasMissingNodes()
        {
            if (asset == null) return false;
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(asset)) return true;
            foreach (AINode node in asset.Nodes)
                if (node == null) return true;
            return false;
        }

        void ShowMoreMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Save"), false, Save);
            menu.AddItem(new GUIContent("Minimap"), showMiniMap, () =>
            {
                showMiniMap = !showMiniMap;
                if (graphView != null) graphView.MiniMapVisible = showMiniMap;
            });
            if (HasMissingNodes()) menu.AddItem(new GUIContent("Remove Missing Nodes"), false, RemoveMissingNodes);
            else menu.AddDisabledItem(new GUIContent("Remove Missing Nodes"));
            menu.DropDown(moreButton.worldBound);
        }

        void OpenAddNode()
        {
            if (graphView == null || asset == null) return;
            graphView.OpenSearch(position.position + graphView.worldBound.center, null);
        }

        void AutoLayout()
        {
            if (asset == null || graphView == null) return;
            AIGraphEditing.MoveNodes(asset, AIGraphLayout.Compute(asset));
            Reload();
            graphView.schedule.Execute(() =>
            {
                if (graphView != null) graphView.FrameAll();
            });
        }

        void OnGraphKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey || evt.altKey || evt.shiftKey || !ShouldHandleShortcut(evt.target as VisualElement)) return;
            if (evt.keyCode == KeyCode.Space)
            {
                OpenAddNode();
                evt.StopImmediatePropagation();
            }
            else if (evt.keyCode == KeyCode.F)
            {
                if (graphView.selection.Count > 0) graphView.FrameSelection();
                else graphView.FrameAll();
                evt.StopImmediatePropagation();
            }
            // The same key press also arrives as a separate character event; GraphView's built-in
            // shortcut handler reacts to that one, so swallow it or Space/F would fire twice.
            else if (evt.character == ' ' || evt.character == 'f' || evt.character == 'F')
            {
                evt.StopImmediatePropagation();
            }
        }

        void TogglePlay()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return;
            }
            EditorApplication.isPaused = !EditorApplication.isPaused;
            RefreshPlayButtons();
        }

        void Step()
        {
            if (!EditorApplication.isPlaying) return;
            EditorApplication.isPaused = true;
            EditorApplication.Step();
            RefreshPlayButtons();
        }

        void RefreshPlayButtons()
        {
            if (playButton == null) return;
            bool running = EditorApplication.isPlaying && !EditorApplication.isPaused;
            playButton.Q<Label>(className: "ai-tb-text").text = running ? AIGlyphs.Pause + " Pause" : AIGlyphs.Play + " Play";
            playButton.EnableInClassList("ai-on", running);
            stepButton.SetEnabled(EditorApplication.isPlaying);
        }

        void OnNodeSelected(string nodeId)
        {
            selectedNodeId = nodeId;
            ShowInspector();
        }

        void OnNodeEdited(string nodeId)
        {
            graphView.RefreshNodes();
            RefreshStatus();
        }

        void OnBlackboardChanged()
        {
            graphView.RefreshNodes();
            ShowInspector(true);
            RefreshStatus();
        }

        void OnGraphChanged()
        {
            if (selectedNodeId != null && (asset == null || asset.GetNode(selectedNodeId) == null)) selectedNodeId = null;
            ShowInspector();
            RefreshStatus();
        }

        void OnUndoRedo()
        {
            if (asset == null || graphView == null) return;
            Reload();
        }

        void OnSelectionChanged()
        {
            if (!EditorApplication.isPlaying || asset == null) return;
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.TryGetComponent(out AIAgent agent) && agent.Graph == asset) SetAgent(agent);
        }

        void OnPlayModeChanged(PlayModeStateChange change)
        {
            RefreshPlayButtons();
            if (change != PlayModeStateChange.EnteredEditMode) return;
            debugger?.SetAgent(debugAgent);
            Reload();
        }

        void OnPauseChanged(PauseState state)
        {
            RefreshPlayButtons();
        }

        void UpdateDebugger()
        {
            if (debugger == null) return;
            debugger.Update();
            blackboardPanel.SetLive(debugger.IsLive ? debugger.Agent.Blackboard : null);
            consolePanel.Refresh();
            inspectorPanel.SetStatus(debugger.StatusLabel(selectedNodeId));
        }

        void Save()
        {
            if (asset != null) AssetDatabase.SaveAssetIfDirty(asset);
        }

        void RemoveMissingNodes()
        {
            if (asset == null) return;
            if (!EditorUtility.DisplayDialog("Remove missing nodes", "Delete every node whose class no longer exists? Their saved data is lost (Undo still works).", "Remove", "Cancel")) return;
            AIMissingNodes.RemoveAll(asset);
            Reload();
        }
    }
}
