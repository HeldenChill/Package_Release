using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities.Core.Data
{
    public abstract class ICharacterDefinition : ScriptableObject
    {
        public class BuildContext{}
        public abstract ICharacterStats BuildRuntime(BuildContext context = null);
    }
}