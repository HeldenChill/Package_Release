using System.Collections.Generic;

namespace Hung.Combat
{
    public static class DamageModifierRegistry
    {
        static readonly List<IDamageModifier> modifiers = new();

        public static IReadOnlyList<IDamageModifier> Modifiers => modifiers;

        public static void Register(IDamageModifier modifier)
        {
            if (modifier != null && !modifiers.Contains(modifier))
            {
                modifiers.Add(modifier);
            }
        }

        public static void Unregister(IDamageModifier modifier)
        {
            if (modifier != null)
            {
                modifiers.Remove(modifier);
            }
        }

        public static void Clear() => modifiers.Clear();

        public static IElementalResistanceProfile ApplyModifiers(
            in DamageDealtContext context,
            IElementalResistanceProfile profile)
        {
            IElementalResistanceProfile result = profile;
            for (int i = 0; i < modifiers.Count; i++)
            {
                result = modifiers[i].ModifyProfile(context, result);
            }

            return result;
        }
    }
}
