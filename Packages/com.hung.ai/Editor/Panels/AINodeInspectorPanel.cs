using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Right panel: node summary (name, category, assembly, live status) and the selected node's own serialized fields, bound to the asset (edits are undoable).</summary>
    public sealed class AINodeInspectorPanel : VisualElement
    {
        const string CoreAssembly = "Hung.AI";
        const string Inactive = "inactive";
        static readonly HashSet<string> HiddenFields = new HashSet<string> { "id", "editorPosition", "childIds", "childId" };
        readonly Action<string> edited;
        readonly ScrollView body;
        SerializedObject serializedObject;
        VisualElement issuesBox;
        Label assemblyValue;
        Label statusValue;
        string status = Inactive;
        AIGraphAsset shownAsset;
        string shownNodeId;
        int shownIndex = -1;

        public AINodeInspectorPanel(Action<string> onEdited)
        {
            edited = onEdited;
            AddToClassList("ai-panel");
            AddToClassList("ai-inspector");
            var header = new VisualElement();
            header.AddToClassList("ai-panel-header");
            var title = new Label("INSPECTOR");
            title.AddToClassList("ai-panel-title");
            title.AddToClassList("ai-f-display");
            header.Add(title);
            Add(header);
            body = new ScrollView();
            body.AddToClassList("ai-panel-body");
            Add(body);
            Show(null, null, null);
        }

        public string AssemblyText => assemblyValue != null ? assemblyValue.text : string.Empty;

        public string StatusText => status;

        /// <summary>Live debugger status of the shown node: inactive, running, success, failure or aborted.</summary>
        public void SetStatus(string value)
        {
            status = string.IsNullOrEmpty(value) ? Inactive : value;
            if (statusValue != null) statusValue.text = status;
        }

        /// <summary>Rebuilds the fields only when the node (or its slot in the asset) changed, or when forced; otherwise just refreshes the warnings, so a field being typed in keeps focus.</summary>
        public void Show(AIGraphAsset asset, string nodeId, IReadOnlyList<string> issues, bool force = false)
        {
            AINode node = asset != null ? asset.GetNode(nodeId) : null;
            int index = node != null ? AIEditorUtil.IndexOfNode(asset, nodeId) : -1;
            if (!force && index >= 0 && serializedObject != null && asset == shownAsset && nodeId == shownNodeId && index == shownIndex)
            {
                SetIssues(issues);
                return;
            }
            body.Clear();
            serializedObject?.Dispose();
            serializedObject = null;
            issuesBox = null;
            assemblyValue = null;
            statusValue = null;
            shownAsset = asset;
            shownNodeId = nodeId;
            shownIndex = index;
            if (index < 0)
            {
                body.Add(Hint("Select a node to edit it."));
                return;
            }
            Type type = node.GetType();
            AINodeTypeInfo info = AINodeTypeCatalog.Describe(type);
            string assembly = type.Assembly.GetName().Name;
            body.Add(InfoField("Node", AINodeTypeCatalog.DisplayName(node), info.Category.ToString(), out _));
            body.Add(InfoField("Assembly", assembly, null, out assemblyValue));
            if (!string.IsNullOrEmpty(info.Description))
            {
                var description = new Label(info.Description);
                description.AddToClassList("ai-inspector-description");
                body.Add(description);
            }
            issuesBox = new VisualElement();
            body.Add(issuesBox);
            SetIssues(issues);
            serializedObject = new SerializedObject(asset);
            SerializedProperty nodeProperty = serializedObject.FindProperty("nodes").GetArrayElementAtIndex(index);
            var fields = new VisualElement();
            SerializedProperty iterator = nodeProperty.Copy();
            SerializedProperty end = nodeProperty.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                if (HiddenFields.Contains(iterator.name)) continue;
                fields.Add(new PropertyField(iterator.Copy()));
            }
            body.Add(fields);
            fields.Bind(serializedObject);
            string trackedId = nodeId;
            fields.TrackSerializedObjectValue(serializedObject, _ => edited?.Invoke(trackedId));
            if (node is ConditionNode condition && condition.AbortType != AbortType.None)
                body.Add(Hint("Conditional abort: while a lower branch runs, this condition is re-checked every tick. When it changes, the running branch gets OnAbort then OnEnd and the tree switches."));
            if (assembly != CoreAssembly)
                body.Add(Hint($"Bridge node from {assembly}. Core Hung.AI never sees it; game code adds nodes like this without editing the package."));
            body.Add(InfoField("Status", status, null, out statusValue));
        }

        static VisualElement InfoField(string caption, string value, string badge, out Label valueLabel)
        {
            var field = new VisualElement();
            field.AddToClassList("ai-insp-field");
            var label = new Label(caption.ToUpperInvariant());
            label.AddToClassList("ai-insp-caption");
            field.Add(label);
            var box = new VisualElement();
            box.AddToClassList("ai-insp-val");
            valueLabel = new Label(value);
            valueLabel.AddToClassList("ai-f-mono");
            box.Add(valueLabel);
            if (!string.IsNullOrEmpty(badge))
            {
                var chip = new Label(badge);
                chip.AddToClassList("ai-chip");
                chip.AddToClassList("ai-f-mono");
                box.Add(chip);
            }
            field.Add(box);
            return field;
        }

        static Label Hint(string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("ai-hint");
            return hint;
        }

        void SetIssues(IReadOnlyList<string> issues)
        {
            if (issuesBox == null) return;
            issuesBox.Clear();
            if (issues != null)
                foreach (string issue in issues) issuesBox.Add(new HelpBox(issue, HelpBoxMessageType.Warning));
        }
    }
}
