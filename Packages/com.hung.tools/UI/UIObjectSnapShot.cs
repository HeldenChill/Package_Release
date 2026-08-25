using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Tool
{
    [Serializable]
    public class UIObjectSnapshot
    {
        [Header("Identity")]
        public string id;
        public string name;
        public string parentId;
        public string hierarchyPath;
        public int siblingIndex;
        public bool activeSelf = true;

        [Header("Prefab Source")]
        public GameObject sourcePrefab;
        public string sourcePrefabPath;

        [Header("Layout")]
        public RectTransformSnapshot rectTransform = new RectTransformSnapshot();

        [Header("Components")]
        public List<UIComponentSnapshot> components = new List<UIComponentSnapshot>();
    }
}
