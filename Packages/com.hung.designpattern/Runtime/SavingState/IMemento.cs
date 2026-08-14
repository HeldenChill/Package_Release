using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern
{
    public interface IMemento
    {
        public int Id
        {
            get;
        }
        public void Restore();
    }
}