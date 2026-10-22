namespace Hung.Combat
{
    public interface IDamageModifier
    {
        IElementalResistanceProfile ModifyProfile(in DamageDealtContext context, IElementalResistanceProfile profile);
    }
}
