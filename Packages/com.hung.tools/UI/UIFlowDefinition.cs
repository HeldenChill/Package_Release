using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace Hung.Tool
{
    [CreateAssetMenu(menuName = "UI/Flow Definition")]
    public class UIFlowDefinition : ScriptableObject
    {
        [Header("Canvas")]
        public string canvasName = "NewCanvas";
        public string nameSpace = "UI";
        public BaseClass baseClass = BaseClass.UISCanvas;
        public string customBaseClass;
        [Header("Generation")]
        public bool useSnapshotWhenAvailable = true;
        public bool preserveImportedLayout = true;
        public bool preserveImportedComponents = true;
        public bool preservePrefabSource = true;
        [Header("Schema Mode")]
        public List<WidgetSpec> widgets = new List<WidgetSpec>();
        public List<FlowStep> openFlow = new List<FlowStep>();
        public List<FlowStep> updateFlow = new List<FlowStep>();
        public List<ClickSpec> clicks = new List<ClickSpec>();

        [Header("Snapshot Mode")]
        public List<UIObjectSnapshot> objectSnapshots = new List<UIObjectSnapshot>();
        public List<UIFieldBindingSnapshot> fieldBindings = new List<UIFieldBindingSnapshot>();

        public bool HasSnapshot()
        {
            return objectSnapshots != null && objectSnapshots.Count > 0;
        }
    }
    
}