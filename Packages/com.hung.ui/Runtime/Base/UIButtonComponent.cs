using System.Collections;
using System.Collections.Generic;


namespace Hung.UI
{
    using System;
    using TMPro;
    using UnityEngine;
    using UnityEngine.U2D;
    using UnityEngine.UI;
    [RequireComponent(typeof(UIButton))]
    public abstract class UIButtonComponent : MonoBehaviour
    {
        [Serializable]
        protected class ImageState
        {
            public Image Image;
            public SpriteAtlas SpriteAtlas;
            public Sprite[] Sprites;
            public bool[] RaycaseTargets = new bool[] { false, true, true, false };
            public Color[] Colors = new Color[] { new Color(1 / 3f, 1 / 3f, 1 / 3f), Color.white, Color.white, Color.white };
            public Image.Type[] Types = new Image.Type[] { Image.Type.Sliced, Image.Type.Sliced, Image.Type.Sliced, Image.Type.Simple };
            public bool[] PreserveAspects = new bool[] { false, false, false, true };
            public Vector2[] Positions;
            public Vector2[] Sizes;
        }
        [Serializable]
        protected class TextState
        {
            public TMP_Text Text;
            public string[] Contents;
            public bool[] Actives;
            public Color[] Colors;
            public Color[] OutlineColors;
            public float[] OutlineWidths;
            public int[] Sizes;
        }
        public abstract void SetState(UIButton.STATE state);
    }
}