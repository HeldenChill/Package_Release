using System;
using System.Collections;
using System.Collections.Generic;


namespace Hung.UI
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    [Serializable]
    public struct UIItemData
    {
        public Sprite Sprite;
        public int Quantity;
        public Color IconColor;
        public bool isHaveX;

        public UIItemData(Sprite Sprite, int Quantity, Color IconColor)
        {
            this.Sprite = Sprite;
            this.Quantity = Quantity;
            this.IconColor = IconColor;
            isHaveX = true;
        }
    }
    public class UIItem : MonoBehaviour
    {
        private const char X_SIGN = 'x';
        [SerializeField]
        protected Image icon;
        [SerializeField]
        protected Image frame;
        [SerializeField]
        protected TMP_Text quantityText;
        [SerializeField]
        protected bool isHaveX = true;
        [SerializeField]
        List<UIAnim> anims;
        protected int quantity = 0;
        public Image Icon => icon;
        public void SetData(int quantity, bool isHaveX = true)
        {
            this.isHaveX = isHaveX;
            this.quantity = quantity;
            if (isHaveX)
            {
                quantityText.text = X_SIGN + quantity.ToString();
            }
            else
            {
                quantityText.text = quantity.ToString();
            }
        }

        public void PlayAnim(UIAnim.ANIM anim)
        {
            for (int i = 0; i < anims.Count; i++)
            {
                anims[i].Play(anim);
            }
        }
        public void SetData(string quantityText)
        {
            this.quantityText.text = quantityText;
        }

        public void SetData(Sprite sprite, bool isActive = true)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(isActive);
        }
        public void SetFrame(Sprite sprite, bool isActive = true)
        {
            frame.sprite = sprite;
            frame.gameObject.SetActive(isActive);
        }

        public void SetData(Color iconColor)
        {
            icon.color = iconColor;
        }

        public void SetData(UIItemData data)
        {
            SetData(data.Sprite);
            SetData(data.Quantity, data.isHaveX);
            SetData(data.IconColor);
        }

    }
}
