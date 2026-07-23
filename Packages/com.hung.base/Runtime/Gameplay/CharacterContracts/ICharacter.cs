using UnityEngine;

namespace Hung.Base
{
    public interface ICharacter
    {
        public Transform Tf { get; }
        public Transform SkinTf { get; }
        public bool IsDie { get; }
        public bool IsDebug { get; }
    }
}
