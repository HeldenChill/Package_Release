using Hung.Base;

namespace Hung.Combat
{
    public class FinalDamageInfo
    {
        public float damage;
        public ELEMENTAL mainType;
        public bool isCrit;

        public FinalDamageInfo(float damage, ELEMENTAL mainType, bool isCrit = false)
        {
            this.damage = damage;
            this.mainType = mainType;
            this.isCrit = isCrit;
        }

        public void Set(float damage, ELEMENTAL mainType, bool isCrit = false)
        {
            this.damage = damage;
            this.mainType = mainType;
            this.isCrit = isCrit;
        }
    }
}
