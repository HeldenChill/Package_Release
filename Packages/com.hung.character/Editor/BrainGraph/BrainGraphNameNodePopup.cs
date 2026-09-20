using UnityEditor;
using UnityEngine;
using Gameplay.Character.BrainSystem;

namespace Gameplay.Character.EditorTools
{
    // Tiny text-input popup used when creating an Action node, so it can be named on the spot
    // (Unity Behavior Graph style). GenericMenu can't host a text field, hence a popup window.
    public sealed class BrainGraphNameNodePopup : EditorWindow
    {
        private BrainGraphEditorWindow owner;
        private BrainGraphNode node;
        private string nodeName;
        private bool focused;

        public static void Show(BrainGraphEditorWindow owner, BrainGraphNode node, string defaultName)
        {
            BrainGraphNameNodePopup popup = CreateInstance<BrainGraphNameNodePopup>();
            popup.owner = owner;
            popup.node = node;
            popup.nodeName = defaultName;
            popup.titleContent = new GUIContent("Name Node");
            Vector2 size = new Vector2(280f, 70f);
            Vector2 center = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) * 0.5f;
            popup.position = new Rect(center - size * 0.5f, size);
            popup.ShowUtility();
        }

        private void OnGUI()
        {
            if (node == null) { Close(); return; }

            EditorGUILayout.LabelField("Node name", EditorStyles.boldLabel);
            GUI.SetNextControlName("NodeNameField");
            nodeName = EditorGUILayout.TextField(nodeName);
            if (!focused) { EditorGUI.FocusTextInControl("NodeNameField"); focused = true; }

            // Enter confirms, Esc cancels.
            Event e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
            bool esc = e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel") || esc) { Close(); return; }
                if (GUILayout.Button("OK") || enter) Confirm();
            }
        }

        private void Confirm()
        {
            if (!string.IsNullOrWhiteSpace(nodeName)) node.title = nodeName.Trim();
            if (owner != null) owner.Repaint();
            Close();
        }
    }
}
