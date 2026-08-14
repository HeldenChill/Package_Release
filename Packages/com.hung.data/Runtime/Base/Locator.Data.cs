namespace Hung.Base
{
    public static partial class Locator
    {
        private static IDataService data;
        public static IDataService Data
        {
            get => data;
            set => data = value;
        }

        public static void ResetDataForTests() => data = null;
    }
}
