namespace Hung.Base
{
    public static partial class Locator
    {
        private static IPiggyBankService piggyBank;
        public static IPiggyBankService PiggyBank
        {
            get => piggyBank;
            set => piggyBank = value;
        }
    }
}
