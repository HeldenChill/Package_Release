using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Builds the GraphView search window tree. Types must be sorted by Path so each group is contiguous.</summary>
    public static class AINodeSearchTree
    {
        public static List<SearchTreeEntry> Build(IReadOnlyList<AINodeTypeInfo> types, Texture icon)
        {
            var tree = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("Create AI Node"), 0) };
            var openedGroups = new HashSet<string>();
            for (int t = 0; t < types.Count; t++)
            {
                AINodeTypeInfo info = types[t];
                string[] parts = info.Path.Split('/');
                for (int depth = 0; depth < parts.Length - 1; depth++)
                {
                    string groupPath = string.Join("/", parts, 0, depth + 1);
                    if (openedGroups.Add(groupPath)) tree.Add(new SearchTreeGroupEntry(new GUIContent(parts[depth]), depth + 1));
                }
                tree.Add(new SearchTreeEntry(new GUIContent(info.Name, icon, info.Description)) { level = parts.Length, userData = info.Type });
            }
            return tree;
        }
    }
}
