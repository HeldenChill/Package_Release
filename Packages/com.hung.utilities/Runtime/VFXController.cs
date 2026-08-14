using UnityEngine;

namespace Hung.Utilities
{
    public class VFXController : MonoBehaviour
    {
        protected Transform target;
        [SerializeField]
        protected ParticleSystem _particleSystem;
        [SerializeField]
        protected Transform tf;
        public ParticleSystem ParticleSystem => _particleSystem;
        public Transform Tf => tf;

        private void OnEnable()
        {
            target = null;
        }

        public void Tracking(Transform target)
        {
            this.target = target;
        }

        private void FixedUpdate()
        {
            if(target != null)
            {
                tf.position = target.position;
            }
        }
    }
}