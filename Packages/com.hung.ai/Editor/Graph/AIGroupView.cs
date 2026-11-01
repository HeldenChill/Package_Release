using UnityEditor.Experimental.GraphView;
namespace Hung.AI.Editor
{
    public sealed class AIGroupView : Group
    {
        public AIGroupView(string groupId, string groupTitle)
        {
            GroupId = groupId;
            title = groupTitle;
            AddToClassList("ai-group");
        }

        public string GroupId { get; }
    }
}
