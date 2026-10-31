namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves PiggyBank through the framework registry.</summary>
        public static IPiggyBankService PiggyBank => ServiceSlots.Get<IPiggyBankService>();
    }
}
