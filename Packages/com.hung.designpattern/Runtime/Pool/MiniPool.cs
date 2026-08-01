using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern
{
    public class MiniPool<T> where T : Component
    {
        private readonly List<T> _listActives = new();
        private int _index;
        private Transform _parent;
        private T _prefab;

        public List<T> GetActiveList()
        {
            return _listActives;
        }

        public void OnInit(T prefab, int amount, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            _index = 0;

            for (int i = 0; i < amount; i++)
            {
                T obj = Object.Instantiate(prefab, parent);
                _listActives.Add(obj);
                Despawn(obj);
            }
        }

        public T Spawn(Vector3 pos, Quaternion rot)
        {
            T go = Spawn();

            go.transform.SetPositionAndRotation(pos, rot);

            return go;
        }

        public T Spawn()
        {
            T go = _index < _listActives.Count ? _listActives[_index] : null;

            if (go == null)
            {
                go = Object.Instantiate(_prefab, _parent);
                _listActives.Add(go);
            }

            _index++;
            go.gameObject.SetActive(true);

            return go;
        }

        public void Despawn(T obj, bool isCollect = false)
        {
            if (!obj.gameObject.activeSelf) return;
            obj.gameObject.SetActive(false);
            if (_index > 0 && !isCollect) _index--;
        }

        public void Collect()
        {
            for (int i = 0; i < _index; i++) Despawn(_listActives[i], true);
            _index = 0;
        }

        public void Release()
        {
            Collect();
            for (int i = 0; i < _listActives.Count; i++) Object.Destroy(_listActives[i].gameObject);
            _listActives.Clear();
            _index = 0;
        }
    }
}
