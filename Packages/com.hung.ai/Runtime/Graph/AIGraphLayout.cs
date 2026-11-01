using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>Editor-only layout: a titled box around nodes. The runtime ignores it.</summary>
    [Serializable]
    public sealed class AIGraphGroup
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Title = "Group";
        public Vector2 Position;
        public List<string> NodeIds = new List<string>();
    }

    /// <summary>Editor-only sticky note. The runtime ignores it.</summary>
    [Serializable]
    public sealed class AIGraphNote
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Title = "Note";
        public string Contents = string.Empty;
        public Rect Position = new Rect(0f, 0f, 200f, 160f);
    }
}
