using System;
namespace Hung.AI
{
    /// <summary>Editor metadata: search-window path ("Category/Name") and tooltip.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AINodeAttribute : Attribute
    {
        public string Path { get; }
        public string Description { get; set; }

        public AINodeAttribute(string path)
        {
            Path = path;
        }
    }
}
