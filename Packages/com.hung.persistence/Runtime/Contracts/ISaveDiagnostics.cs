namespace Hung.Base.Persistence
{
    public interface ISaveDiagnostics
    {
        void Report(SaveDiagnostic diagnostic);
    }

    public sealed class NullSaveDiagnostics : ISaveDiagnostics
    {
        public static readonly NullSaveDiagnostics Instance = new NullSaveDiagnostics();

        private NullSaveDiagnostics()
        {
        }

        public void Report(SaveDiagnostic diagnostic)
        {
        }
    }
}
