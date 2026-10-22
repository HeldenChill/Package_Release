using System;
using Hung.DesignPattern;
using UnityEngine;

namespace Hung.Skill
{
    public class ColliderController : GameUnit
    {
        public event Action<ColliderController, Collider> _OnTriggerEnter;

        protected void OnTriggerEnter(Collider other)
        {
            _OnTriggerEnter?.Invoke(this, other);
        }
    }
}
