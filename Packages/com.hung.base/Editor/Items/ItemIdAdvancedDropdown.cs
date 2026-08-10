using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Hung.Base.Editor
{
    internal sealed class ItemIdAdvancedDropdown : AdvancedDropdown
    {
        private readonly IReadOnlyList<ItemIdEditorOption> options;
        private readonly int selectedIndex;
        private readonly Action<ItemId> onSelected;

        public ItemIdAdvancedDropdown(
            AdvancedDropdownState state,
            IReadOnlyList<ItemIdEditorOption> options,
            ItemId selected,
            Action<ItemId> onSelected)
            : base(state)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            selectedIndex = FindSelectedIndex(options, selected);
            minimumSize = new Vector2(420f, 320f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("ItemId");
            var groups = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal);
            for (int index = 0; index < options.Count; index++)
            {
                ItemIdEditorOption option = options[index];
                AdvancedDropdownItem parent = GetOrCreateGroup(root, groups, option.GroupPath);
                var item = new AdvancedDropdownItem(option.Label) { id = index };
                if (index == selectedIndex)
                    item.icon = EditorGUIUtility.IconContent("FilterSelectedOnly").image as Texture2D;
                parent.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item.id >= 0 && item.id < options.Count)
                onSelected(options[item.id].Id);
        }

        private static int FindSelectedIndex(IReadOnlyList<ItemIdEditorOption> options, ItemId selected)
        {
            for (int index = 0; index < options.Count; index++)
                if (options[index].Id == selected) return index;
            return -1;
        }

        private static AdvancedDropdownItem GetOrCreateGroup(
            AdvancedDropdownItem root,
            IDictionary<string, AdvancedDropdownItem> groups,
            string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            if (groups.TryGetValue(path, out AdvancedDropdownItem existing)) return existing;

            AdvancedDropdownItem parent = root;
            string currentPath = string.Empty;
            foreach (string segment in path.Split('/'))
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";
                if (!groups.TryGetValue(currentPath, out AdvancedDropdownItem group))
                {
                    group = new AdvancedDropdownItem(segment);
                    groups.Add(currentPath, group);
                    parent.AddChild(group);
                }
                parent = group;
            }

            return parent;
        }
    }
}
