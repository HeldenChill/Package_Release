using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Left panel: edit the graph's blackboard keys, or show live values of the debugged agent (bool keys flip on click).</summary>
    public sealed class AIBlackboardPanel : VisualElement
    {
        const long FlashMilliseconds = 700;
        readonly Action changed;
        readonly ScrollView list;
        readonly Button addButton;
        readonly List<(string keyId, Label label)> liveLabels = new List<(string keyId, Label label)>();
        readonly Dictionary<string, VisualElement> liveRows = new Dictionary<string, VisualElement>();
        readonly Dictionary<string, double> flashUntil = new Dictionary<string, double>();
        AIGraphAsset asset;
        Blackboard live;

        public AIBlackboardPanel(Action onChanged)
        {
            changed = onChanged;
            AddToClassList("ai-panel");
            AddToClassList("ai-blackboard");
            var header = new VisualElement();
            header.AddToClassList("ai-panel-header");
            var title = new Label("BLACKBOARD");
            title.AddToClassList("ai-panel-title");
            title.AddToClassList("ai-f-display");
            header.Add(title);
            var right = new VisualElement();
            right.AddToClassList("ai-panel-header-right");
            var scope = new Label("scope: Agent");
            scope.AddToClassList("ai-chip");
            scope.AddToClassList("ai-f-mono");
            right.Add(scope);
            addButton = new Button(ShowAddMenu) { text = "+", tooltip = "Add a key" };
            right.Add(addButton);
            header.Add(right);
            Add(header);
            list = new ScrollView();
            list.AddToClassList("ai-panel-body");
            Add(list);
            RegisterCallback<DetachFromPanelEvent>(_ => Attach(null));
            Rebuild();
        }

        public void SetGraph(AIGraphAsset graph)
        {
            asset = graph;
            Attach(null);
            Rebuild();
        }

        /// <summary>Non-null switches to live values; null returns to editing.</summary>
        public void SetLive(Blackboard blackboard)
        {
            if (blackboard == live)
            {
                RefreshLive();
                return;
            }
            Attach(blackboard);
            Rebuild();
        }

        /// <summary>Flips a Bool key on a runtime blackboard. The graph asset is never touched. False when the key is missing or not Bool.</summary>
        public static bool FlipBool(Blackboard blackboard, string keyId)
        {
            if (blackboard == null) return false;
            int index = BlackboardValueFields.FindRuntimeIndex(blackboard, keyId);
            if (index < 0 || blackboard.GetValueType(index) != BlackboardValueType.Bool) return false;
            return blackboard.Set(index, !blackboard.Get<bool>(index));
        }

        void Attach(Blackboard blackboard)
        {
            if (live != null) live.KeyChanged -= OnKeyChanged;
            live = blackboard;
            if (live != null) live.KeyChanged += OnKeyChanged;
        }

        void Rebuild()
        {
            list.Clear();
            liveLabels.Clear();
            liveRows.Clear();
            flashUntil.Clear();
            addButton.SetEnabled(asset != null && live == null);
            if (asset == null)
            {
                list.Add(Hint("No graph open."));
                return;
            }
            IReadOnlyList<BlackboardKey> keys = asset.Blackboard.Keys;
            if (keys.Count == 0) list.Add(Hint("No keys. Press + to add one."));
            for (int i = 0; i < keys.Count; i++) list.Add(live != null ? LiveRow(keys[i]) : EditRow(keys[i]));
            if (live != null) list.Add(Hint("Keys live on the graph asset; every agent gets its own copy. Click a bool key to flip it."));
            RefreshLive();
        }

        VisualElement EditRow(BlackboardKey key)
        {
            string keyId = key.Id;
            var row = new VisualElement();
            row.AddToClassList("ai-key-row");
            var top = new VisualElement();
            top.AddToClassList("ai-key-row-top");
            var handle = new Label("≡") { tooltip = "Drag onto a key field in the node inspector" };
            handle.AddToClassList("ai-key-handle");
            handle.RegisterCallback<MouseDownEvent>(_ => StartDrag(keyId));
            var nameField = new TextField { value = key.Name, isDelayed = true };
            nameField.AddToClassList("ai-key-name");
            nameField.RegisterValueChangedCallback(e => Rename(keyId, e.newValue));
            var typeField = new EnumField(key.Type);
            typeField.AddToClassList("ai-key-type");
            typeField.RegisterValueChangedCallback(e => Edit(keyId, "Change Blackboard Key Type", k =>
            {
                k.Type = (BlackboardValueType)e.newValue;
                k.DefaultValue = default;
            }, true));
            var remove = new Button(() => RemoveKey(keyId)) { text = "×", tooltip = "Delete key" };
            top.Add(handle);
            top.Add(nameField);
            top.Add(typeField);
            top.Add(remove);
            row.Add(top);
            if (key.Type == BlackboardValueType.Enum)
            {
                var enumName = new TextField("Enum type") { value = key.EnumTypeName ?? string.Empty, isDelayed = true, tooltip = "Full or assembly-qualified enum type name." };
                enumName.RegisterValueChangedCallback(e => Edit(keyId, "Edit Blackboard Enum Type", k => k.EnumTypeName = e.newValue, true));
                row.Add(enumName);
            }
            row.Add(BlackboardValueFields.Create(key, key.DefaultValue, v => Edit(keyId, "Edit Blackboard Default", k => k.DefaultValue = v, false), "Default", false));
            return row;
        }

        VisualElement LiveRow(BlackboardKey key)
        {
            string keyId = key.Id;
            var row = new VisualElement();
            row.AddToClassList("ai-key-row");
            var top = new VisualElement();
            top.AddToClassList("ai-key-row-top");
            var name = new Label(key.Name);
            name.AddToClassList("ai-key-name");
            var type = new Label(key.Type.ToString());
            type.AddToClassList("ai-key-typetag");
            type.AddToClassList("ai-f-mono");
            top.Add(name);
            top.Add(type);
            row.Add(top);
            var value = new Label();
            value.AddToClassList("ai-key-live");
            value.AddToClassList("ai-f-mono");
            row.Add(value);
            if (key.Type == BlackboardValueType.Bool)
            {
                row.AddToClassList("ai-key-toggle");
                row.focusable = true;
                row.tooltip = "Click (or Enter) to flip it on the running agent";
                row.RegisterCallback<ClickEvent>(_ => Flip(keyId));
                row.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) Flip(keyId);
                });
            }
            liveLabels.Add((keyId, value));
            liveRows[keyId] = row;
            return row;
        }

        void Flip(string keyId)
        {
            if (FlipBool(live, keyId)) RefreshLive();
        }

        // Can fire every frame for a key like a target position: only mark the row; the 100 ms poll
        // (SetLive) refreshes labels and clears the flash, so a key that keeps changing stays lit.
        void OnKeyChanged(string keyId)
        {
            if (!liveRows.TryGetValue(keyId, out VisualElement row)) return;
            row.AddToClassList("ai-key-changed");
            flashUntil[keyId] = EditorApplication.timeSinceStartup + FlashMilliseconds / 1000.0;
        }

        void RefreshLive()
        {
            if (live == null) return;
            foreach ((string keyId, Label label) in liveLabels)
            {
                int index = BlackboardValueFields.FindRuntimeIndex(live, keyId);
                label.text = index < 0 ? "(not in runtime copy)" : BlackboardValueFields.FormatLive(live, index);
            }
            double now = EditorApplication.timeSinceStartup;
            foreach (KeyValuePair<string, VisualElement> pair in liveRows)
            {
                if (flashUntil.TryGetValue(pair.Key, out double until) && until <= now)
                {
                    pair.Value.RemoveFromClassList("ai-key-changed");
                    flashUntil.Remove(pair.Key);
                }
            }
        }

        static Label Hint(string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("ai-hint");
            return hint;
        }

        void Edit(string keyId, string undoLabel, Action<BlackboardKey> mutate, bool rebuild)
        {
            BlackboardKey key = asset != null ? asset.Blackboard.FindById(keyId) : null;
            if (key == null) return;
            Undo.RecordObject(asset, undoLabel);
            mutate(key);
            EditorUtility.SetDirty(asset);
            if (rebuild) Rebuild();
            changed?.Invoke();
        }

        void Rename(string keyId, string wanted)
        {
            if (asset == null || string.IsNullOrWhiteSpace(wanted))
            {
                Rebuild();
                return;
            }
            string unique = AIEditorUtil.UniqueKeyName(asset.Blackboard, wanted.Trim(), keyId);
            Edit(keyId, "Rename Blackboard Key", k => k.Name = unique, unique != wanted);
        }

        void RemoveKey(string keyId)
        {
            if (asset == null) return;
            Undo.RecordObject(asset, "Delete Blackboard Key");
            asset.Blackboard.Remove(keyId);
            EditorUtility.SetDirty(asset);
            Rebuild();
            changed?.Invoke();
        }

        void AddKey(BlackboardValueType type)
        {
            if (asset == null) return;
            Undo.RecordObject(asset, "Add Blackboard Key");
            asset.Blackboard.Add(AIEditorUtil.UniqueKeyName(asset.Blackboard, "New" + type), type);
            EditorUtility.SetDirty(asset);
            Rebuild();
            changed?.Invoke();
        }

        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (BlackboardValueType type in Enum.GetValues(typeof(BlackboardValueType)))
            {
                BlackboardValueType captured = type;
                menu.AddItem(new GUIContent(type.ToString()), false, () => AddKey(captured));
            }
            menu.DropDown(addButton.worldBound);
        }

        void StartDrag(string keyId)
        {
            BlackboardKey key = asset != null ? asset.Blackboard.FindById(keyId) : null;
            if (key == null) return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(BlackboardKeyRefDrawer.DragDataKey, keyId);
            DragAndDrop.objectReferences = new UnityEngine.Object[0];
            DragAndDrop.StartDrag(key.Name);
        }
    }
}
