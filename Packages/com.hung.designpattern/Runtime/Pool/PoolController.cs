
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hung.DesignPattern
{
    [DefaultExecutionOrder(-100)]
    public class PoolController : Singleton<PoolController>
    {
        [SerializeField]
        private PoolAmount[] _poolAmounts;
        [SerializeField]
        private ParticleAmount[] _particleAmounts;
        
        public void Awake()
        {
            for (int i = 0; i < _particleAmounts.Length; i++)
                ParticlePool.Preload(_particleAmounts[i].prefab, _particleAmounts[i].amount, _particleAmounts[i].root);

            for (int i = 0; i < _poolAmounts.Length; i++)
                SimplePool.Preload(_poolAmounts[i].prefab, _poolAmounts[i].amount, _poolAmounts[i].root, _poolAmounts[i].collect, _poolAmounts[i].clamp);
            DontDestroyOnLoad(gameObject);
        }
    }
}
