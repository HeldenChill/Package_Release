using System.Collections.Generic;
using UnityEngine;

namespace StyleTextureConverter
{
    [CreateAssetMenu(menuName = "Hung/Tools/Style Texture Profile", fileName = "StyleProfile_New")]
    public sealed class StyleProfile : ScriptableObject
    {
        [Tooltip("Appended to the source file name. Must be non-empty so the source is never overwritten.")]
        public string outputSuffix = "_Styled";

        [SerializeReference] public List<StyleStep> steps = new List<StyleStep>();
    }
}
