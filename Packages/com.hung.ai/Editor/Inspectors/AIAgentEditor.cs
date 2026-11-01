using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    [CustomEditor(typeof(AIAgent))]
    public sealed class AIAgentEditor : UnityEditor.Editor
    {
        VisualElement overridesBox;
        VisualElement liveBox;

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            if (overridesBox != null && target != null) RebuildOverrides();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new PropertyField(serializedObject.FindProperty("graph")));
            root.Add(new PropertyField(serializedObject.FindProperty("autoTick")));
            root.Add(new PropertyField(serializedObject.FindProperty("tickInterval")));
            root.Add(new PropertyField(serializedObject.FindProperty("randomSeed")));
            var header = new Label("Blackboard overrides");
            header.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            header.style.marginTop = 6;
            root.Add(header);
            overridesBox = new VisualElement();
            root.Add(overridesBox);
            liveBox = new VisualElement();
            liveBox.style.marginTop = 6;
            root.Add(liveBox);
            root.Add(new Button(OpenGraph) { text = "Open Graph Editor" });
            root.TrackPropertyValue(serializedObject.FindProperty("graph"), _ => RebuildOverrides());
            RebuildOverrides();
            root.schedule.Execute(RefreshLive).Every(250);
            return root;
        }

        void OpenGraph()
        {
            var agent = (AIAgent)target;
            if (agent.Graph != null) AIGraphEditorWindow.Open(agent.Graph, agent);
        }

        void RebuildOverrides()
        {
            overridesBox.Clear();
            var agent = (AIAgent)target;
            AIGraphAsset graph = agent.Graph;
            if (graph == null)
            {
                overridesBox.Add(new HelpBox("Assign a graph to edit per-agent blackboard values.", HelpBoxMessageType.Info));
                return;
            }
            IReadOnlyList<BlackboardKey> keys = graph.Blackboard.Keys;
            if (keys.Count == 0) overridesBox.Add(new Label("This graph has no blackboard keys."));
            for (int i = 0; i < keys.Count; i++) overridesBox.Add(OverrideRow(agent, keys[i]));
            int stale = agent.Overrides.FindAll(o => o == null || graph.Blackboard.FindById(o.KeyId) == null).Count;
            if (stale > 0) overridesBox.Add(new Button(() => RemoveStale(agent, graph)) { text = $"Remove {stale} override(s) for deleted keys" });
        }

        VisualElement OverrideRow(AIAgent agent, BlackboardKey key)
        {
            string keyId = key.Id;
            BlackboardValue defaultValue = key.DefaultValue;
            BlackboardKeyOverride existing = agent.Overrides.Find(o => o != null && o.KeyId == keyId);
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            var toggle = new Toggle { value = existing != null, tooltip = "Override the graph default for this agent" };
            VisualElement field = BlackboardValueFields.Create(key, existing != null ? existing.Value : defaultValue, v => SetOverride(agent, keyId, v), key.Name, !EditorUtility.IsPersistent(agent));
            field.style.flexGrow = 1;
            field.SetEnabled(existing != null);
            toggle.RegisterValueChangedCallback(e =>
            {
                if (e.newValue) SetOverride(agent, keyId, defaultValue);
                else RemoveOverride(agent, keyId);
                RebuildOverrides();
            });
            row.Add(toggle);
            row.Add(field);
            return row;
        }

        void RefreshLive()
        {
            if (liveBox == null) return;
            liveBox.Clear();
            var agent = target as AIAgent;
            if (agent == null || !EditorApplication.isPlaying || !agent.IsInitialized) return;
            liveBox.Add(new Label(agent.IsPaused ? "Live values (paused)" : "Live values"));
            Blackboard blackboard = agent.Blackboard;
            for (int i = 0; i < blackboard.Count; i++)
                liveBox.Add(new Label($"{blackboard.GetKeyName(i)}: {BlackboardValueFields.FormatLive(blackboard, i)}"));
        }

        static void SetOverride(AIAgent agent, string keyId, BlackboardValue value)
        {
            Undo.RecordObject(agent, "Edit AI Blackboard Override");
            BlackboardKeyOverride entry = agent.Overrides.Find(o => o != null && o.KeyId == keyId);
            if (entry == null)
            {
                entry = new BlackboardKeyOverride { KeyId = keyId };
                agent.Overrides.Add(entry);
            }
            entry.Value = value;
            MarkChanged(agent);
        }

        static void RemoveOverride(AIAgent agent, string keyId)
        {
            Undo.RecordObject(agent, "Remove AI Blackboard Override");
            agent.Overrides.RemoveAll(o => o != null && o.KeyId == keyId);
            MarkChanged(agent);
        }

        void RemoveStale(AIAgent agent, AIGraphAsset graph)
        {
            Undo.RecordObject(agent, "Remove Stale AI Overrides");
            agent.Overrides.RemoveAll(o => o == null || graph.Blackboard.FindById(o.KeyId) == null);
            MarkChanged(agent);
            RebuildOverrides();
        }

        static void MarkChanged(AIAgent agent)
        {
            EditorUtility.SetDirty(agent);
            PrefabUtility.RecordPrefabInstancePropertyModifications(agent);
        }
    }
}
