using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    using Hung.UI;
    using Hung.Utilities;

    public class TextsStateButtonComponent : UIButtonComponent
    {
        [SerializeField]
        TextState[] Data;
        List<FontMaterialProp> FontProps;
        protected void Awake()
        {
            FontProps = new List<FontMaterialProp>();
            for(int i = 0; i < Data.Length; i++)
            {
                FontProps.Add(Data[i].Text.GetComponent<FontMaterialProp>());
            }
        }
        public override void SetState(UIButton.STATE state)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                if (Data[i].Contents.Length > (int)state)
                {
                    Data[i].Text.text = Data[i].Contents[(int)state];
                }
                if (Data[i].Actives.Length > (int)state)
                {
                    Data[i].Text.gameObject.SetActive(Data[i].Actives[(int)state]);
                }
                if (Data[i].Colors.Length > (int)state)
                {
                    Data[i].Text.color = Data[i].Colors[(int)state];
                }
                if (Data[i].Sizes.Length > (int)state)
                {
                    Data[i].Text.fontSize = Data[i].Sizes[(int)state];
                }
                if(Data[i].OutlineColors.Length > (int)state)
                {
                    FontProps[i].SetupData(Data[i].OutlineColors[(int)state], Data[i].OutlineWidths[(int)state]);
                }

            }
        }
    }
}