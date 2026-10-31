namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Data through the framework registry.</summary>
        public static IDataService Data => ServiceSlots.Get<IDataService>();

        public static void ResetDataForTests() => ServiceSlots.Unregister<IDataService>(ServiceSlots.Get<IDataService>());
    }
}
