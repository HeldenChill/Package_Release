using System.Collections.Generic;

namespace Hung.AutoTest
{
    public sealed class AutoTestCommandLine
    {
        private static readonly HashSet<string> RecognizedFlags = new HashSet<string>(System.StringComparer.Ordinal)
        {
            "-rcScenario", "-rcRun", "-rcOutput", "-rcPhase", "-rcProfile", "-rcState",
        };

        public string ScenarioId { get; private set; }
        public string RunId { get; private set; }
        public string OutputPath { get; private set; }
        public string Phase { get; private set; }
        public string Profile { get; private set; }
        public string StatePath { get; private set; }
        public string ValidationError { get; private set; }

        public bool IsRuntimeConfidenceRun
        {
            get { return !string.IsNullOrEmpty(ScenarioId); }
        }

        public static AutoTestCommandLine Parse(string[] args)
        {
            var parsed = new AutoTestCommandLine();

            if (args == null)
                return parsed;

            var seenFlags = new HashSet<string>(System.StringComparer.Ordinal);

            for (int i = 0; i < args.Length; i++)
            {
                string flag = args[i];

                if (!RecognizedFlags.Contains(flag))
                    continue;

                if (!seenFlags.Add(flag))
                {
                    parsed.ValidationError = "RC_CLI_DUPLICATE_FLAG";
                    continue;
                }

                bool hasValue = i + 1 < args.Length && !RecognizedFlags.Contains(args[i + 1]) && !IsUnityFlag(args[i + 1]);
                if (!hasValue)
                {
                    parsed.ValidationError = "RC_CLI_VALUE_MISSING";
                    continue;
                }

                string value = args[++i];

                switch (flag)
                {
                    case "-rcScenario":
                        parsed.ScenarioId = value;
                        break;
                    case "-rcRun":
                        parsed.RunId = value;
                        break;
                    case "-rcOutput":
                        parsed.OutputPath = value;
                        break;
                    case "-rcPhase":
                        parsed.Phase = value;
                        break;
                    case "-rcProfile":
                        parsed.Profile = value;
                        break;
                    case "-rcState":
                        parsed.StatePath = value;
                        break;
                }
            }

            return parsed;
        }

        private static bool IsUnityFlag(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Length > 1 && value[0] == '-' && !char.IsDigit(value[1]);
        }
    }
}
