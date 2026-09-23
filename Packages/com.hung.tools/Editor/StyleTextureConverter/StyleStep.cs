using System;

namespace StyleTextureConverter
{
    /// <summary>
    /// One pixel operation in a StyleProfile. Profiles store steps by type name ([SerializeReference]):
    /// renaming a subclass needs [UnityEngine.Scripting.APIUpdating.MovedFrom] or every profile using it loses the step.
    /// </summary>
    [Serializable]
    public abstract class StyleStep
    {
        public bool enabled = true;

        public virtual string DisplayName => GetType().Name;

        public abstract void Apply(StyleBuffer buf);
    }
}
