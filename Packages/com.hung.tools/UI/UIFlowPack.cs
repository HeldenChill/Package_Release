using System.Collections.Generic;
using UnityEngine;

namespace Hung.Tool
{
    [CreateAssetMenu(menuName = "UI/Flow Pack", fileName = "UIFlowPack")]
    public class UIFlowPack : ScriptableObject
    {
        public string packName;
        public string gameId;
        public List<UIFlowDefinition> definitions = new List<UIFlowDefinition>();

        [Tooltip("Shared assets not directly referenced by a specific UIFlowDefinition but required by this UI pack.")]
        public List<UnityEngine.Object> sharedAssets = new List<UnityEngine.Object>();
    }
}