using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern
{
    public interface IOriginator
    {
        IMemento Save();
    }
}