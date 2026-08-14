using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern {
    public interface InitData { }
    public interface DespawnData { }
    public interface IConstructor
    {
        public void OnInit<T>(T data) where T : InitData;
        public void OnDespawn<T>(T data) where T : DespawnData;
    }
}