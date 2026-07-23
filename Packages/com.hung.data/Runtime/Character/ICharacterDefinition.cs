using UnityEngine;

namespace Hung.Data
{
    public abstract class ICharacterDefinition : ScriptableObject
    {
        public string Id;

        public class BuildContext { }

        public abstract ICharacterStats BuildRuntime(BuildContext context = null);
    }
}
