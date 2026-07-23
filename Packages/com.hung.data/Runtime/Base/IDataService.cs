using UnityEngine;

namespace Hung.Base
{
    public interface IDataService
    {
        public T GetData<T>(int index = 0) where T : class;
        public T GetSOData<T>() where T : ScriptableObject;
        public T GetUnit<T>(int type) where T : class;
        public void Save();
    }
}
