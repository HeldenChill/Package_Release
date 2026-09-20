using UnityEditor;
using UnityEngine;

namespace Gameplay.Character.EditorTools
{
    // "New Node Type" popup: name + category, then runs BrainGraphNodeCodegen to scaffold a new
    // IBrainGraphNode script and add its enum value. Unity recompiles and the kind appears in the
    // grouped create menu.
    public sealed class BrainGraphNewNodePopup : EditorWindow
    {
        private string nodeName = "MyNode";
        private int categoryIndex = 2; // default Action
        private bool focused;
        private string error;

        public static void Show()
        {
            BrainGraphNewNodePopup popup = CreateInstance<BrainGraphNewNodePopup>();
            popup.titleContent = new GUIContent("New Node Type");
            Vector2 size = new Vector2(320f, 120f);
            Vector2 center = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) * 0.5f;
            popup.position = new Rect(center - size * 0.5f, size);
            popup.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create a new node type", EditorStyles.boldLabel);
            GUI.SetNextControlName("NewNodeName");
            nodeName = EditorGUILayout.TextField("Name", nodeName);
            categoryIndex = EditorGUILayout.Popup("Category", categoryIndex, BrainGraphNodeCodegen.Categories);
            if (!focused) { EditorGUI.FocusTextInControl("NewNodeName"); focused = true; }

            if (!string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox(error, MessageType.Error);

            Event e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
            bool esc = e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel") || esc) { Close(); return; }
                if (GUILayout.Button("Create") || enter) Confirm();
            }
        }

        private void Confirm()
        {
            string category = BrainGraphNodeCodegen.Categories[categoryIndex];
            if (BrainGraphNodeCodegen.Create(nodeName, category, out error))
                Close();
            // else: error shown, popup stays open.
        }
    }
}
