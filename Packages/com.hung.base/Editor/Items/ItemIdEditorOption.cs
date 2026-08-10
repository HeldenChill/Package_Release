using Hung.Base;

namespace Hung.Base.Editor
{
    public readonly struct ItemIdEditorOption
    {
        public ItemIdEditorOption(ItemId id, string label, string groupPath)
        {
            Id = id;
            Label = string.IsNullOrEmpty(label) ? id.Value : label;
            GroupPath = groupPath ?? string.Empty;
            SearchText = id.Value;
        }

        public ItemId Id { get; }
        public string Label { get; }
        public string GroupPath { get; }
        public string SearchText { get; }
    }
}
