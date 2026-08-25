using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Tool
{
    /// <summary>
    /// Maps generated serialized fields to object snapshot ids.
    /// Example: nextLevelBtns -> [button_0_id, button_1_id]
    /// </summary>
    [Serializable]
    public class UIFieldBindingSnapshot
    {
        public string fieldName;
        public bool isList;
        public string referenceTypeName;
        public List<string> objectIds = new List<string>();
    }
}
