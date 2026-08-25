using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Tool
{
    [Serializable]
    public class ClickSpec
    {
        public string buttonField; // "nextLevelBtns"
        public int index;
        public List<FlowStep> body;
    }
}
