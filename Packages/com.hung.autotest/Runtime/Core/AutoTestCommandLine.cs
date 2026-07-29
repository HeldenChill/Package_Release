namespace Hung.AutoTest
{
    public sealed class AutoTestCommandLine
    {
        public string ScenarioId { get; private set; }
        public string RunId { get; private set; }
        public string OutputPath { get; private set; }
        public string Phase { get; private set; }
        public string Profile { get; private set; }

        public bool IsRuntimeConfidenceRun
        {
            get { return !string.IsNullOrEmpty(ScenarioId); }
        }

        public static AutoTestCommandLine Parse(string[] args)
        {
            var parsed = new AutoTestCommandLine();

            if (args == null)
                return parsed;

            for (int i = 0; i < args.Length; i++)
            {
                string flag = args[i];
                string value = i + 1 < args.Length ? args[i + 1] : string.Empty;

                switch (flag)
                {
                    case "-rcScenario":
                        parsed.ScenarioId = value;
                        i++;
                        break;
                    case "-rcRun":
                        parsed.RunId = value;
                        i++;
                        break;
                    case "-rcOutput":
                        parsed.OutputPath = value;
                        i++;
                        break;
                    case "-rcPhase":
                        parsed.Phase = value;
                        i++;
                        break;
                    case "-rcProfile":
                        parsed.Profile = value;
                        i++;
                        break;
                }
            }

            return parsed;
        }
    }
}
