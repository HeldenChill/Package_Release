using NUnit.Framework;
using UnityEngine;
namespace Hung.AI.Editor.Tests
{
    public class AIEditorVisualTests
    {
        [Test]
        public void CategoryOf_PerceptionPath_IsPerception()
        {
            Assert.AreEqual(AINodeCategory.Perception, AINodeTypeCatalog.CategoryOf(typeof(PerceptionProbeNode)));
            Assert.AreEqual(AINodeCategory.Perception, AINodeTypeCatalog.Describe(typeof(PerceptionProbeNode)).Category);
            Assert.IsTrue(AINodeTypeCatalog.IsPerceptionPath("Character/Perception/Sense World"));
            Assert.IsTrue(AINodeTypeCatalog.IsPerceptionPath("Perception/Hear"));
            Assert.IsFalse(AINodeTypeCatalog.IsPerceptionPath("Actions/Wait"));
            Assert.IsFalse(AINodeTypeCatalog.IsPerceptionPath(null));
        }

        [Test]
        public void CategoryOf_CoreNodes_Unchanged()
        {
            Assert.AreEqual(AINodeCategory.Action, AINodeTypeCatalog.CategoryOf(typeof(WaitNode)));
            Assert.AreEqual(AINodeCategory.Condition, AINodeTypeCatalog.CategoryOf(typeof(IsSetNode)));
            Assert.AreEqual(AINodeCategory.Composite, AINodeTypeCatalog.CategoryOf(typeof(SequenceNode)));
            Assert.AreEqual(AINodeCategory.Decorator, AINodeTypeCatalog.CategoryOf(typeof(InverterNode)));
            Assert.AreEqual(AINodeCategory.Missing, AINodeTypeCatalog.CategoryOf(null));
        }

        [Test]
        public void DisplayName_UsesCatalogNameUnlessTitled()
        {
            Assert.AreEqual("Wait", AINodeTypeCatalog.DisplayName(new WaitNode()));
            Assert.AreEqual("Patrol Pause", AINodeTypeCatalog.DisplayName(new WaitNode { Title = "Patrol Pause" }));
            Assert.AreEqual(string.Empty, AINodeTypeCatalog.DisplayName(null));
        }

        [Test]
        public void Fonts_LoadAndMonoHasEveryGlyph()
        {
            foreach (string file in AIEditorUtil.FontFiles)
                Assert.IsNotNull(AIEditorUtil.LoadFont(file), file);
            Font mono = AIEditorUtil.LoadFont("JetBrainsMono-Regular.ttf");
            foreach (char c in AIGlyphs.All)
                Assert.IsTrue(mono.HasCharacter(c), "JetBrains Mono lacks U+" + ((int)c).ToString("X4"));
        }

        [Test]
        public void NodeView_ConditionWithAbortBoth_ShowsCardAndChip()
        {
            var view = new AINodeView("c", AINodeCategory.Condition, null, null);
            view.Refresh(new IsSetNode { Id = "c", AbortType = AbortType.Both }, false, null);
            Assert.AreEqual("Is Set", view.NameText);
            Assert.AreEqual("CONDITION", view.CategoryText);
            Assert.AreEqual("abort: Both", view.AbortChipText);
            Assert.IsTrue(view.AbortChipVisible);
            Assert.IsTrue(view.ClassListContains("ai-no-sub"));
        }

        [Test]
        public void NodeView_NoAbort_HidesChip_AndCustomTitleShowsSub()
        {
            var view = new AINodeView("w", AINodeCategory.Action, null, null);
            view.Refresh(new WaitNode { Id = "w", Title = "Idle Wait" }, true, null);
            Assert.AreEqual("Idle Wait", view.NameText);
            Assert.AreEqual(string.Empty, view.AbortChipText);
            Assert.IsFalse(view.AbortChipVisible);
            Assert.IsFalse(view.ClassListContains("ai-no-sub"));
            Assert.IsTrue(view.ClassListContains("ai-root"));
            Assert.AreEqual("abort: Lower", AINodeView.AbortText(AbortType.LowerPriority));
            Assert.AreEqual("abort: Self", AINodeView.AbortText(AbortType.Self));
        }

        [Test]
        public void NodeView_Perception_HasCategoryClass()
        {
            var view = new AINodeView("p", AINodeTypeCatalog.CategoryOf(typeof(PerceptionProbeNode)), null, null);
            Assert.IsTrue(view.ClassListContains("ai-perception"));
            Assert.AreEqual("PERCEPTION", view.CategoryText);
        }

        [Test]
        public void EdgeControl_BendsAtHalfHeight()
        {
            var points = new[] { new Vector2(0f, 0f), new Vector2(0f, 10f), new Vector2(50f, 90f), new Vector2(50f, 100f) };
            AIEdgeControl.ApplyVerticalS(points);
            Assert.AreEqual(new Vector2(0f, 0f), points[0]);
            Assert.AreEqual(new Vector2(0f, 50f), points[1]);
            Assert.AreEqual(new Vector2(50f, 50f), points[2]);
            Assert.AreEqual(new Vector2(50f, 100f), points[3]);
        }

        [Test]
        public void Window_HasZoomLabelAndLegend()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            AIGraphEditorWindow window = null;
            try
            {
                asset.AddNode(new WaitNode { Id = "w" });
                asset.RootNodeId = "w";
                window = AIGraphEditorWindow.Open(asset);
                StringAssert.EndsWith("%", window.GraphView.ZoomText);
                UnityEngine.UIElements.VisualElement legend = UnityEngine.UIElements.UQueryExtensions.Q(window.GraphView, null, "ai-legend");
                Assert.IsNotNull(legend);
                Assert.AreEqual(4, legend.childCount);
            }
            finally
            {
                if (window != null) window.Close();
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Inspector_ShowsAssemblyAndStatus()
        {
            var asset = ScriptableObject.CreateInstance<AIGraphAsset>();
            try
            {
                asset.AddNode(new WaitNode { Id = "w" });
                var panel = new AINodeInspectorPanel(null);
                panel.Show(asset, "w", null);
                Assert.AreEqual("Hung.AI", panel.AssemblyText);
                Assert.AreEqual("inactive", panel.StatusText);
                panel.SetStatus("running");
                Assert.AreEqual("running", panel.StatusText);
                panel.SetStatus(null);
                Assert.AreEqual("inactive", panel.StatusText);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
