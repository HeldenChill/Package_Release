using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace Hung.Utilities
{
    public class FontMaterialProp : MonoBehaviour
    {
        [BoxGroup("Outline")]
        [SerializeField]
        bool isUseOutline;
        [BoxGroup("Outline")]
        [SerializeField]
        [ColorUsage(true, true)]
        Color outlineColor;
        [BoxGroup("Outline")]
        [SerializeField]
        float outlineWidth;

        TMP_Text textComponent;
        Material matInstance;

        protected LocalKeyword outlineKeyWord;
        protected IEnumerator Start()
        {
            textComponent = GetComponent<TMP_Text>();
            yield return null;
            matInstance = textComponent.fontMaterial; // This creates an instance
            outlineKeyWord = new LocalKeyword(matInstance.shader, "OUTLINE_ON");
            UpdateOutline();
        }

        public void UpdateOutline()
        {
            if (textComponent == null || matInstance == null)
                return;
            outlineWidth = Mathf.Clamp01(outlineWidth);
            if (isUseOutline)
            {
                matInstance.EnableKeyword(outlineKeyWord);
                matInstance.SetFloat("_OutlineWidth", outlineWidth); // Must be > 0
                matInstance.SetColor("_OutlineColor", outlineColor);  // Set your desired color
            }
            else
            {
                matInstance.SetFloat("_OutlineWidth", 0f);
                matInstance.DisableKeyword(outlineKeyWord);
            }
            textComponent.fontMaterial = matInstance;
            textComponent.UpdateMeshPadding();
            textComponent.havePropertiesChanged = true;
            textComponent.SetMaterialDirty();
            textComponent.SetVerticesDirty();
            textComponent.ForceMeshUpdate();
        }

        public void SetupData(Color outlineColor, float outlineWidth, bool isUseOutline = true)
        {
            this.isUseOutline = isUseOutline;
            this.outlineWidth = outlineWidth;
            this.outlineColor = outlineColor;
            UpdateOutline();
        }
    }
}
