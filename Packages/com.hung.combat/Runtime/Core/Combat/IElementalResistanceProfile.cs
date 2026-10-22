using Hung.Base;

namespace Hung.Combat
{
    public interface IElementalResistanceProfile
    {
        float GetMultiplier(ELEMENTAL type);
    }
}
