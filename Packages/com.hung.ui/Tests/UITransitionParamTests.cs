using NUnit.Framework;
using Hung.UI;

namespace Hung.UI.Tests
{
    // No transition-param ScriptableObject exists in com.hung.ui (searched -- CreateAssetMenu count: 0).
    // UIAnim.Propertys (Runtime/Base/UIAnim.cs) is the closest testable default-value target: a plain
    // serializable POCO, no MonoBehaviour/scene needed.
    public class UITransitionParamTests
    {
        [Test]
        public void Propertys_DefaultTime_NonDegenerate()
        {
            var props = new UIAnim.Propertys();

            Assert.Greater(props.Time, 0f);
        }
    }
}
