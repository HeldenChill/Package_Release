using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Tool
{
    [Serializable]
    public class UIComponentSnapshot
    {
        public string typeName;
        public string assemblyQualifiedTypeName;
        public bool enabled = true;

        [TextArea(3, 12)]
        public string editorJson;

        [Tooltip("True for components we intentionally do not recreate, e.g. Transform/RectTransform/CanvasRenderer/root UI script.")]
        public bool skipRecreate;

        [Tooltip("Explicit asset references used for package export and safer restore. Examples: Sprite, Material, TMP_FontAsset, AnimatorController, AudioClip.")]
        public List<UIComponentAssetReference> assetReferences = new List<UIComponentAssetReference>();
    }
}