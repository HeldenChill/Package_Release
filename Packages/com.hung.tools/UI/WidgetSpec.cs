using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Tool
{
    public enum WidgetType
    {
        GameObject,
        Transform,
        Image,
        Button,
        UIButton,
        TMP_Text,
        UIPack,
        ListItemShow,
        ParticleSystem,
        Custom
    }
    [Serializable]
    public class WidgetSpec
    {
        public WidgetType type;      // UIButton, TMP_Text, UIPack, ListItemShow, Image, FlyTransform
        public string fieldName; // "nextLevelBtns"
        public bool isList;
        public int listCount; // for nextLevelBtns = 2
        public GameObject sourcePrefab; // BasicButton.prefab, Pack.prefab, …
        [Tooltip("Used when type = Custom. Example: MyCustomButton, UI.RewardSlot, etc.")]
        public string customTypeName;

        [Tooltip("Optional. Used by reverse-import to remember where this object lived in the original prefab hierarchy.")]
        public string hierarchyPath;

        [Tooltip("Optional object ids in objectSnapshots. Builder can use these to recreate exact layout/components for this widget.")]
        public List<string> snapshotObjectIds = new List<string>();
    }
}
