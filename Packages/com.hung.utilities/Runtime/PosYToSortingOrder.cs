using System.Collections.Generic;
using UnityEngine;

namespace Hung.Utilities
{
    public class PosYToSortingOrder : MonoBehaviour
    {
        public static float RATE = 100;
        [SerializeField]
        List<SpriteRenderer> sprites;
        [SerializeField]
        List<MeshRenderer> renderers;
        protected void Update()
        {
            foreach (SpriteRenderer sprite in sprites)
            {
                if(sprite.gameObject.activeInHierarchy)
                    sprite.sortingOrder = (int)(RATE * sprite.transform.position.y);
            }
            foreach(MeshRenderer render in renderers)
            {
                if (render.gameObject.activeInHierarchy)
                    render.sortingOrder = (int)(RATE * render.transform.position.y);
            }
        }
    }
}
