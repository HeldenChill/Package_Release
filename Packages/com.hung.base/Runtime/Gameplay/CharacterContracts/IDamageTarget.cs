using Hung.DesignPattern;
using UnityEngine;

namespace Hung.Base
{
    public interface IDamageTarget
    {
        Transform Tf { get; }
        CHARACTER_TYPE Type { get; }
        GameUnit Source { get; }
    }
}
