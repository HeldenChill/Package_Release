using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Search window behind Space, right-click Create Node, and edges dropped on empty space.</summary>
    public sealed class AINodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        AIGraphView view;
        Texture2D indent;

        public void Init(AIGraphView owner)
        {
            view = owner;
            indent = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            indent.SetPixel(0, 0, Color.clear);
            indent.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            return AINodeSearchTree.Build(AINodeTypeCatalog.All, indent);
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (view == null || !(entry.userData is Type type)) return false;
            view.CreateNodeAt(type, view.ScreenToGraph(context.screenMousePosition));
            return true;
        }

        void OnDestroy()
        {
            if (indent != null) DestroyImmediate(indent);
        }
    }
}
