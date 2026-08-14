using UnityEngine;

namespace Hung.DesignPattern
{
    public abstract class GameUnit : MonoBehaviour
    {
        [SerializeField]
        protected Transform tf;
        [SerializeField]
        protected Transform skinTf;
        [SerializeField]
        protected PoolType poolType;
        public PoolType PoolType => poolType;
        public Transform Tf => tf;
        public Transform SkinTf => skinTf;
        public RectTransform RectTf => (RectTransform)tf;
        public virtual void OnDespawn() { }
    }
}
