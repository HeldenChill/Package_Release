using System.Collections.Generic;
using Hung.Base;
using UnityEngine;

namespace Hung.Skill
{
    public class EffectVFXController : MonoBehaviour
    {
        [SerializeField]
        protected List<Transform> levelVfxs;
        public void SetType(int level, ELEMENTAL effect)
        {
            foreach (Transform vfx in levelVfxs)
            {
                vfx.gameObject.SetActive(false);
            }
            levelVfxs[level].gameObject.SetActive(true);
            for (int i = 0; i < levelVfxs[level].transform.childCount; i++)
            {
                Transform child = levelVfxs[level].transform.GetChild(i);
                child.gameObject.SetActive(false);
            }
            levelVfxs[level].GetChild((int)effect).gameObject.SetActive(true);
        }
    }
}
