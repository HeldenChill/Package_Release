using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

[ExecuteInEditMode]
public class AtlasSprite : MonoBehaviour
{
    public SpriteAtlas atlas;
    public Image image;
    public Sprite sprite;
    private void Awake()
    {

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            if (image != null)
            {
                sprite = image.sprite;
            }
        }
        else
        {
            if (atlas != null)
                image.sprite = atlas.GetSprite(sprite.name);
        }
        
#else
        if(atlas != null)
            image.sprite = atlas.GetSprite(sprite.name);
#endif
    }


}
