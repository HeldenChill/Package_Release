using System.Collections.Generic;

namespace Hung.AutoTest
{
    public static class AutoTestAssertionFactory
    {
        public static void CreateAssertions(AutoTestCaseData testCase, List<IAutoTestAssertion> output)
        {
            output.Clear();

            if (testCase == null || testCase.assertions == null)
                return;

            for (int i = 0; i < testCase.assertions.Count; i++)
            {
                AutoTestAssertionConfig config = testCase.assertions[i];
                if (config == null || !config.enabled)
                    continue;

                IAutoTestAssertion assertion = Create(config);
                if (assertion != null)
                    output.Add(assertion);
            }
        }

        private static IAutoTestAssertion Create(AutoTestAssertionConfig config)
        {
            // Game-agnostic assertions live here; everything game-specific is
            // registered by the game's glue (see AutoTestAssertionRegistry and
            // PetVsMonsterAutoTestGlue).
            switch (config.type)
            {
                case AutoTestAssertionType.NoExceptionLog:
                    return new NoExceptionLogAssertion(config);
                case AutoTestAssertionType.ScenarioStarted:
                    return new ScenarioStartedAssertion(config);
                case AutoTestAssertionType.ScenarioTimeout:
                    return new ScenarioTimeoutAssertion(config);
                case AutoTestAssertionType.NoNaNTransform:
                    return new NoNaNTransformAssertion(config);
                default:
                    return AutoTestAssertionRegistry.TryCreate(config);
            }
        }
    }
}
