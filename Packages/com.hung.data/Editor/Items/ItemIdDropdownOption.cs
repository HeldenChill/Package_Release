using Hung.Base;

namespace Hung.Data.Editor
{
    public readonly struct ItemIdDropdownOption
    {
        public ItemIdDropdownOption(ItemId id, string groupPath)
        {
            Id = id;
            GroupPath = groupPath;
            SearchText = id.Value;
        }

        public ItemId Id { get; }
        public string GroupPath { get; }
        public string SearchText { get; }
    }
}
